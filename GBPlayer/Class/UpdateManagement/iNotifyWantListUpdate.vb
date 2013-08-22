Imports System.Xml
Public Interface iNotifyWantListUpdate
    Event IdWantlistAdded(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate)
    Event IdWantlistRemoved(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate)
    Event IdWantlistUpdated(ByVal Document As XmlDocument, ByVal Transmitter As iNotifyWantListUpdate)
    Function NotifyAddIdWantlist(ByVal Id As String) As Boolean
    Function NotifyRemoveIdWantlist(ByVal id As String) As Boolean
    Function NotifyUpdateWantList(ByVal Document As XmlDocument) As Boolean
End Interface
