// 工具方法：根据时间范围和颗粒度生成 periods
export function generatePeriods(startDate, endDate, granularity) {
  const periods = []
  const start = new Date(startDate)
  const end = new Date(endDate)

  const pad = (n) => (n < 10 ? '0' + n : n)

  let current = new Date(start)

  while (current <= end) {
    if (granularity === 'day') {
      periods.push(`${current.getFullYear()}-${pad(current.getMonth() + 1)}-${pad(current.getDate())}`)
      current.setDate(current.getDate() + 1)
    } else if (granularity === 'week') {
      const weekStart = new Date(current)
      const weekEnd = new Date(current)
      weekEnd.setDate(current.getDate() + 6)
      periods.push(
        `${weekStart.getFullYear()}-${pad(weekStart.getMonth() + 1)}-${pad(weekStart.getDate())} ~ ${weekEnd.getFullYear()}-${pad(weekEnd.getMonth() + 1)}-${pad(weekEnd.getDate())}`
      )
      current.setDate(current.getDate() + 7)
    } else if (granularity === 'month') {
      periods.push(`${current.getFullYear()}-${pad(current.getMonth() + 1)}`)
      current.setMonth(current.getMonth() + 1)
    } else if (granularity === 'quarter') {
      const quarter = Math.floor(current.getMonth() / 3) + 1
      periods.push(`${current.getFullYear()}-Q${quarter}`)
      current.setMonth(current.getMonth() + 3)
    } else if (granularity === 'year') {
      periods.push(`${current.getFullYear()}`)
      current.setFullYear(current.getFullYear() + 1)
    } else {
      break
    }
  }

  return periods
}

