/** Prices are stored in cents on the backend and edited in whole units in the UI. */
export const centsToUnits = (cents: number) => cents / 100

export const unitsToCents = (units: number | null | undefined) => Math.round((units ?? 0) * 100)

export const formatMoney = (cents: number) => centsToUnits(cents).toFixed(2)
