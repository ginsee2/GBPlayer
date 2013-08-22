Imports System.Xml
Public Class UpdateManager
    Private CustomerCollectionList As List(Of iNotifyCollectionUpdate)
    Private CustomerWantList As List(Of iNotifyWantListUpdate)

    '---------------------------------------------------------------------------------------------
    '-------------------GESTION DES MISES A JOUR DE LA COLLECTION---------------------------------
    '---------------------------------------------------------------------------------------------
    Public Function SubscribeCollectionPosts(ByVal Element As iNotifyCollectionUpdate) As Boolean
        If CustomerCollectionList Is Nothing Then CustomerCollectionList = New List(Of iNotifyCollectionUpdate)
        If CustomerCollectionList.IndexOf(Element) = -1 Then
            CustomerCollectionList.Add(Element)
            AddHandler Element.IdCollectionAdded, AddressOf IdCollectionAdded
            AddHandler Element.IdCollectionRemoved, AddressOf IdCollectionRemoved
            AddHandler Element.IdDiscogsCollectionAdded, AddressOf IdDiscogsCollectionAdded
            AddHandler Element.IdDiscogsCollectionRemoved, AddressOf IdDiscogsCollectionRemoved
            Return True
        End If
        Return False
    End Function
    Public Function UnSubscribeCollectionPosts(ByVal Element As iNotifyCollectionUpdate) As Boolean
        If CustomerCollectionList IsNot Nothing Then
            If CustomerCollectionList.IndexOf(Element) <> -1 Then
                CustomerCollectionList.Remove(Element)
                RemoveHandler Element.IdCollectionAdded, AddressOf IdCollectionAdded
                RemoveHandler Element.IdCollectionRemoved, AddressOf IdCollectionRemoved
                RemoveHandler Element.IdDiscogsCollectionAdded, AddressOf IdDiscogsCollectionAdded
                RemoveHandler Element.IdDiscogsCollectionRemoved, AddressOf IdDiscogsCollectionRemoved
                Return True
            End If
        End If
        Return False
    End Function
    Sub IdCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
        CustomerCollectionList.ForEach(Sub(i As iNotifyCollectionUpdate)
                                           If i IsNot Transmitter Then i.NotifyAddIdCollection(id)
                                       End Sub)
        Debug.Print("Collection add")
    End Sub
    Sub IdCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
        CustomerCollectionList.ForEach(Sub(i As iNotifyCollectionUpdate)
                                           If i IsNot Transmitter Then i.NotifyRemoveIdCollection(id)
                                       End Sub)
        Debug.Print("Collection remove")
    End Sub
    Sub IdDiscogsCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
        CustomerCollectionList.ForEach(Sub(i As iNotifyCollectionUpdate)
                                           If i IsNot Transmitter Then i.NotifyAddIdDiscogsCollection(id)
                                       End Sub)
        Debug.Print("element add")
    End Sub
    Sub IdDiscogsCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate)
        CustomerCollectionList.ForEach(Sub(i As iNotifyCollectionUpdate)
                                           If i IsNot Transmitter Then i.NotifyRemoveIdDiscogsCollection(id)
                                       End Sub)
        Debug.Print("element remove")
    End Sub

    '---------------------------------------------------------------------------------------------
    '-------------------GESTION DES MISES A JOUR DE LA WANTLIST-----------------------------------
    '---------------------------------------------------------------------------------------------
    Public Function SubscribeWantlistPosts(ByVal Element As iNotifyWantListUpdate) As Boolean
        If CustomerWantList Is Nothing Then CustomerWantList = New List(Of iNotifyWantListUpdate)
        If CustomerWantList.IndexOf(Element) = -1 Then
            CustomerWantList.Add(Element)
            AddHandler Element.IdWantlistAdded, AddressOf IdWantlistAdded
            AddHandler Element.IdWantlistRemoved, AddressOf IdWantlistRemoved
            AddHandler Element.IdWantlistUpdated, AddressOf IdWantlistUpdated
            Return True
        End If
        Return False
    End Function
    Public Function UnSubscribeWantlistPosts(ByVal Element As iNotifyWantListUpdate) As Boolean
        If CustomerWantList IsNot Nothing Then
            If CustomerWantList.IndexOf(Element) <> -1 Then
                CustomerWantList.Remove(Element)
                RemoveHandler Element.IdWantlistAdded, AddressOf IdWantlistAdded
                RemoveHandler Element.IdWantlistRemoved, AddressOf IdWantlistRemoved
                Return True
            End If
        End If
        Return False
    End Function
    Sub IdWantlistAdded(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate)
        CustomerWantList.ForEach(Sub(i As iNotifyWantListUpdate)
                                     If i IsNot Transmitter Then i.NotifyAddIdWantlist(id)
                                 End Sub)
        Debug.Print("element add want")
    End Sub
    Sub IdWantlistRemoved(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate)
        CustomerWantList.ForEach(Sub(i As iNotifyWantListUpdate)
                                     If i IsNot Transmitter Then i.NotifyRemoveIdWantlist(id)
                                 End Sub)
        Debug.Print("element remove want")
    End Sub
    Sub IdWantlistUpdated(ByVal Document As XmlDocument, ByVal Transmitter As iNotifyWantListUpdate)
        CustomerWantList.ForEach(Sub(i As iNotifyWantListUpdate)
                                     If i IsNot Transmitter Then i.NotifyUpdateWantList(Document)
                                 End Sub)
        Debug.Print("wantlist update")
    End Sub
End Class
