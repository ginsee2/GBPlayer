Public Interface iNotifyCollectionUpdate
    Event IdCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
    Event IdCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
    Event IdDiscogsCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
    Event IdDiscogsCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
    Function NotifyAddIdCollection(ByVal Id As String) As Boolean
    Function NotifyRemoveIdCollection(ByVal id As String) As Boolean
    Function NotifyAddIdDiscogsCollection(ByVal Id As String) As Boolean
    Function NotifyRemoveIdDiscogsCollection(ByVal id As String) As Boolean
End Interface
