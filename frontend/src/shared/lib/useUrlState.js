import { useSearchParams } from 'react-router-dom'
export function useUrlState(name, initial) {
  const [params, setParams] = useSearchParams()
  const raw = params.get(name)
  const value = typeof initial === 'number'
    ? Math.max(1, Math.floor(Number(raw) || initial))
    : raw ?? initial
  const setValue = (next) => setParams(previous => {
    const result = new URLSearchParams(previous)
    if (next === '' || next === initial) result.delete(name)
    else result.set(name, String(next))
    return result
  })
  return [value, setValue]
}
