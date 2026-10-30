using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;

namespace HomeService.Application.Event;

public static class RoombaActivatedEvent
{
    private static readonly List<Action<PatchRoombaStateRequest>> Handlers = [];
    private static readonly Lock Gate = new Lock();

    #region Subscribe
    public static void Subscribe(Action<PatchRoombaStateRequest> handler)
    {
        lock (Gate)
            if (!Handlers.Contains(handler))
                Handlers.Add(handler);
    }
    #endregion

    #region Unsubscribe
    public static void Unsubscribe(Action<PatchRoombaStateRequest> handler)
    {
        lock (Gate)
            Handlers.Remove(handler);
    }
    #endregion

    #region Publish
    public static void Publish(PatchRoombaStateRequest patchRoombaStateRequest)
    {
        Action<PatchRoombaStateRequest>[] snapshot;
        lock (Gate)
            snapshot = [.. Handlers];

        foreach (Action<PatchRoombaStateRequest> handler in snapshot)
        {
            try
            {
                handler(patchRoombaStateRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RoombaActivatedEvent -> Publish -> {ex.Message}");
            }
        }
    }
    #endregion
}
