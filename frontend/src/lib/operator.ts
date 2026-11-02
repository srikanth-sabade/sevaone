export type AssistedChannel = 'AJSK' | 'BSK' | 'K1' | 'B1' | 'G1'

export const assistedChannels: { code: AssistedChannel; name: string; full: string; authority: string }[] = [
  { code: 'AJSK', name: 'AJSK', full: 'Atalji Jana Snehi Kendra', authority: 'Revenue Department' },
  { code: 'BSK', name: 'BSK', full: 'Bangalore One / Seva Kendra', authority: 'BBMP / EDCS' },
  { code: 'K1', name: 'Karnataka One', full: 'Karnataka One counters', authority: 'Directorate of EDCS' },
  { code: 'B1', name: 'Bangalore One', full: 'Bangalore One centres', authority: 'BBMP' },
  { code: 'G1', name: 'Grama One', full: 'Grama One rural centres', authority: 'RDPR' },
]

export type OperatorSession = {
  channel: AssistedChannel
  channelName: string
  operatorId: string
  centreCode: string
  centreName: string
  walletBalance?: number
  email?: string | null
  activeCitizenId?: string | null
}

/** Maps BFF /operator/me into the shape used by Operator UI. */
export function fromOperatorMe(me: {
  operatorId: string
  operatorName: string
  centreCode: string
  centreName: string
  channel: string
  walletBalance: number
  email?: string | null
  activeCitizenId?: string | null
}): OperatorSession {
  const channel = (assistedChannels.find((c) => c.code === me.channel)?.code ?? 'K1') as AssistedChannel
  const meta = assistedChannels.find((c) => c.code === channel)!
  return {
    channel,
    channelName: meta.full,
    operatorId: me.operatorId,
    centreCode: me.centreCode,
    centreName: me.centreName || me.centreCode,
    walletBalance: me.walletBalance,
    email: me.email,
    activeCitizenId: me.activeCitizenId,
  }
}
