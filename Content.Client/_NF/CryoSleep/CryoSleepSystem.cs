using Content.Shared._NF.CryoSleep;

namespace Content.Client._NF.CryoSleep;

public sealed class CryoSleepSystem : SharedCryoSleepSystem
{
    public event Action<ReturnToBodyStatus>? WakeupResponse;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<WakeupRequestMessage.Response>(OnWakeupResponse);
    }

    public void RequestWakeup()
    {
        RaiseNetworkEvent(new WakeupRequestMessage());
    }

    private void OnWakeupResponse(WakeupRequestMessage.Response response)
    {
        WakeupResponse?.Invoke(response.Status);
    }
}
