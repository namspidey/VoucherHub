import test from 'node:test'
import assert from 'node:assert/strict'
import { apiRequest, configureSession } from '../shared/api/httpClient.js'
import { beginRedemption, finishRedemption, getPendingKey } from '../features/redemption/pendingRequests.js'
import { redeemWithPendingKey } from '../features/redemption/redeemWithPendingKey.js'

const originalFetch = globalThis.fetch
test.afterEach(() => { globalThis.fetch = originalFetch })

test('public request does not send the stored bearer token', async () => {
  configureSession({ getToken: () => 'private-token', clearSession() {} })
  globalThis.fetch = async (_url, options) => {
    assert.equal(options.headers.get('Authorization'), null)
    return Response.json({ items: [] })
  }
  assert.deepEqual(await apiRequest('/campaigns', { public: true }), { items: [] })
})

test('ProblemDetails is parsed and the real backend error code is preserved', async () => {
  globalThis.fetch = async () => new Response(JSON.stringify({ code: 'OUT_OF_STOCK', title: 'Conflict' }), {
    status: 409, headers: { 'Content-Type': 'application/problem+json' },
  })
  await assert.rejects(apiRequest('/redeem'), error => error.status === 409 && error.code === 'OUT_OF_STOCK' && error.message !== 'Conflict')
})

test('validation errors are exposed for forms', async () => {
  globalThis.fetch = async () => Response.json({ errors: { Email: ['Email required.'] } }, { status: 400 })
  await assert.rejects(apiRequest('/register'), error => error.fields.Email[0] === 'Email required.')
})

test('a delayed 401 from an old token does not erase a newer login', async () => {
  let token = 'old', cleared = false
  configureSession({ getToken: () => token, clearSession: () => { cleared = true } })
  globalThis.fetch = async () => { token = 'new'; return new Response(null, { status: 401 }) }
  await assert.rejects(apiRequest('/users/me'), error => error.status === 401)
  assert.equal(cleared, false)
})

test('401 for the current token clears the session', async () => {
  let cleared = false
  configureSession({ getToken: () => 'current', clearSession: () => { cleared = true } })
  globalThis.fetch = async () => new Response(null, { status: 401 })
  await assert.rejects(apiRequest('/users/me'))
  assert.equal(cleared, true)
})

test('abort is not translated into a network error', async () => {
  globalThis.fetch = async () => { throw new DOMException('Cancelled', 'AbortError') }
  await assert.rejects(apiRequest('/campaigns'), error => error.name === 'AbortError')
})

test('unsafe bigint numbers are rejected instead of silently displayed rounded', async () => {
  globalThis.fetch = async () => new Response('{"balance":9007199254740993}', { headers: { 'Content-Type': 'application/json' } })
  await assert.rejects(apiRequest('/users/me'), error => error.code === 'UNSAFE_INTEGER')
})

test('retry retains a pending key, scoped by user and offer; completed intention can start anew', () => {
  const values = new Map()
  const storage = { getItem: key => values.get(key) || null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) }
  const first = beginRedemption('user-a', 'offer-a', storage)
  assert.equal(beginRedemption('user-a', 'offer-a', storage), first)
  assert.notEqual(beginRedemption('user-b', 'offer-a', storage), first)
  assert.notEqual(beginRedemption('user-a', 'offer-b', storage), first)
  assert.equal(getPendingKey('user-a', 'offer-a', storage), first)
  finishRedemption('user-a', 'offer-a', storage)
  assert.equal(getPendingKey('user-a', 'offer-a', storage), null)
  assert.notEqual(beginRedemption('user-a', 'offer-a', storage), first)
})

test('lost response/server failure/rate limit retain the intention key until successful replay', async () => {
  const values = new Map()
  const storage = { getItem: key => values.get(key) || null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) }
  const keys = []
  for (const status of [0, 500, 429]) {
    await assert.rejects(redeemWithPendingKey('u', 'o', async (_offer, key) => {
      keys.push(key)
      throw Object.assign(new Error('Unknown outcome'), { status })
    }, storage))
  }
  const result = await redeemWithPendingKey('u', 'o', async (_offer, key) => { keys.push(key); return { id: 'replayed-voucher' } }, storage)
  assert.equal(new Set(keys).size, 1)
  assert.equal(result.id, 'replayed-voucher')
  assert.equal(getPendingKey('u', 'o', storage), null)
})

test('confirmed business rejection ends that intention', async () => {
  const values = new Map()
  const storage = { getItem: key => values.get(key) || null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) }
  await assert.rejects(redeemWithPendingKey('u', 'o', async () => {
    throw Object.assign(new Error('OUT_OF_STOCK'), { status: 409 })
  }, storage))
  assert.equal(getPendingKey('u', 'o', storage), null)
})
