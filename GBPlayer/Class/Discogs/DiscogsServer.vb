Option Compare Text

Imports System.IO
Imports Newtonsoft.Json.Bson
Imports Newtonsoft.Json.Linq
Imports Newtonsoft.Json
Imports System.Net
Imports System.Reflection
Imports System.Xml
Imports System.Threading
Imports System.Windows.Threading

'***********************************************************************************************
'----------------------------CLASSE PERMET DE GERER LES INFORMATIONS VENANT DE DISCOGS----------
'***********************************************************************************************
Public Class DiscogsServer
    Structure StockageAction
        Public UserName As String
        Public id As String
        Public Action As String
        Public DelegateSub As DelegateRequestResult
        Public Sub New(ByVal NewUser As String, ByVal Newid As String, ByVal NewAction As String, ByVal NewDelegateSub As DelegateRequestResult)
            UserName = NewUser
            id = Newid
            Action = NewAction
            DelegateSub = NewDelegateSub
        End Sub
    End Structure

    Private Const GBAU_NOMDOSSIER_STOCKAGEDATA = "GBDev\GBPlayer\Data\Discogs"
    Private Const Site = "api.discogs.com"
    Private Shared _ListeTacheAExecuter As New List(Of StockageAction)
    Private Shared NewThread As Delegate_TacheAsynchrone
    Private Shared TacheEnCours As Boolean
    Private Delegate Sub Delegate_TacheAsynchrone()
    Public Delegate Sub DelegateRequestResult(ByVal Result As String, ByVal IdRelease As String)

    '***********************************************************************************************
    '----------------------------------FONCTION PUBLIQUE-------------------------------------------
    '***********************************************************************************************
    ' ------------------------------Retrouve Noms des fichier XML----------------------------------
    Public Shared Sub RequestGet_UserProfile(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "UserProfile", DelegateSub)
    End Sub
    ' ------------------------------Retrouve Noms des fichier XML----------------------------------
    Public Shared Function FilePathCollectionFolders(ByVal UserName As String) As String
        Return GetNomFichierXML(UserName, "CollectionFolderlist")
    End Function
    Public Shared Function FilePathCollectionList(ByVal UserName As String) As String
        Return GetNomFichierXML(UserName, "Collectionlist")
    End Function
    Public Shared Function FilePathWantList(ByVal UserName As String) As String
        Return GetNomFichierXML(UserName, "Wantlist")
    End Function
    Public Shared Function FileOrdersList(ByVal UserName As String) As String
        Return GetNomFichierXML(UserName, "OrdersList")
    End Function
    ' -----------------Fonctions pour la gestion des folders de la collection Discogs--------------
    Public Shared Sub RequestGet_CollectionFolderListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "FolderGet", DelegateSub)
    End Sub
    Public Shared Sub RequestAdd_CollectionFolder(ByVal UserName As String, ByVal FolderName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, FolderName, "FolderAdd", DelegateSub)
    End Sub
    Public Shared Sub RequestDelete_CollectionFolder(ByVal UserName As String, ByVal FolderId As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, FolderId, "FolderDelete", DelegateSub)
    End Sub
    ' -----------------Fonctions pour la gestion du contenu de la collection Discogs---------------
    Public Shared Sub RequestGet_CollectionListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "CollectionUpdate", DelegateSub)
    End Sub
    Public Shared Sub RequestAdd_CollectionId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "CollectionAdd", DelegateSub)
    End Sub
    Public Shared Sub RequestDelete_CollectionId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "CollectionDelete", DelegateSub)
    End Sub
    Public Shared Sub RequestMove_CollectionId(ByVal UserName As String, ByVal idRelease As String, ByVal instance As String,
                                               ByVal idSourceFolder As String, ByVal idDestinationFolder As String,
                                               ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idRelease & "/" & instance & "/" & idSourceFolder & "/" & idDestinationFolder, "CollectionMove", DelegateSub)
    End Sub
    ' -----------------Fonctions pour la gestion de la Wantlist Discogs----------------------------
    Public Shared Sub RequestGet_WantListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "wantUpdate", DelegateSub)
    End Sub
    Public Shared Sub RequestGet_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "WantGet", DelegateSub)
    End Sub
    Public Shared Sub RequestAdd_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "WantAdd", DelegateSub)
    End Sub
    Public Shared Sub RequestDelete_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "WantDelete", DelegateSub)
    End Sub
    ' -----------------Fonctions pour la gestion de la SellList Discogs----------------------------
    Public Shared Sub RequestGet_SellListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "SellUpdate", DelegateSub)
    End Sub
    Public Shared Sub RequestAdd_SellListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idrelease, "SellAdd", DelegateSub)
    End Sub
    Public Shared Sub RequestDelete_SellListId(ByVal UserName As String, ByVal idlisting As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, idlisting, "SellDelete", DelegateSub)
    End Sub    '***********************************************************************************************
    Public Shared Sub RequestChange_SellListId(ByVal Input As String, ByVal idListing As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(Input, idListing, "SellChange", DelegateSub)
    End Sub
    ' -----------------Fonctions pour la gestion de la SellList Discogs----------------------------
    Public Shared Sub RequestGet_OrderListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult)
        DiscogsRequest(UserName, "", "OrdersList", DelegateSub)
    End Sub
    '-------------------------TACHE DE FOND POUR TRAITEMENT DES REQUETES---------------------------
    '***********************************************************************************************
    Private Shared Sub DiscogsRequest(ByVal UserName As String, ByVal id As String, ByVal Action As String, ByVal DelegateSub As DelegateRequestResult)
        _ListeTacheAExecuter.Add(New StockageAction(UserName, id, Action, DelegateSub))
        If Not TacheEnCours Then
            TacheEnCours = True
            NewThread = New Delegate_TacheAsynchrone(AddressOf DelegateDiscogsRequest)
            NewThread.BeginInvoke(Nothing, Nothing)
        End If
    End Sub
    Private Shared Sub DelegateDiscogsRequest()
        Do
            Thread.Sleep(1000)
            Dim cde As StockageAction = _ListeTacheAExecuter.First
            _ListeTacheAExecuter.Remove(_ListeTacheAExecuter.First)
            Select Case cde.Action
                Case "UserProfile"
                    Get_UserProfile(cde.UserName, cde.DelegateSub)
                Case "FolderGet"
                    Get_CollectionFolderListAll(cde.UserName, cde.DelegateSub)
                Case "FolderAdd"
                    Add_CollectionFolder(cde.UserName, cde.id, cde.DelegateSub)
                Case "FolderDelete"
                    Delete_CollectionFolder(cde.UserName, cde.id, cde.DelegateSub)
                Case "CollectionUpdate"
                    Get_CollectionListAll(cde.UserName, cde.DelegateSub)
                 Case "CollectionAdd"
                    Add_CollectionId(cde.UserName, cde.id, cde.DelegateSub)
                Case "CollectionDelete"
                    Delete_CollectionId(cde.UserName, cde.id, cde.DelegateSub)
                Case "CollectionMove"
                    Dim TabArg() As String = Split(cde.id, "/")
                    Move_CollectionId(cde.UserName, TabArg(0), TabArg(1), TabArg(2), TabArg(3), cde.DelegateSub)
                Case "WantUpdate"
                    Get_WantListAll(cde.UserName, cde.DelegateSub)
                Case "WantGet"
                    Get_WantListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "WantAdd"
                    Add_WantListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "WantDelete"
                    Delete_WantListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "SellUpdate"
                    Get_SellListAll(cde.UserName, cde.DelegateSub)
                Case "SellAdd"
                    Add_SellListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "SellDelete"
                    Delete_SellListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "SellChange"
                    Change_SellListId(cde.UserName, cde.id, cde.DelegateSub)
                Case "OrdersList"
                    Get_OrderListAll(cde.UserName, cde.DelegateSub)
            End Select
        Loop While _ListeTacheAExecuter.Count > 0
        TacheEnCours = False
    End Sub
    ' -----------------Requetes pour la gestion des FOLDERS Discogs---------------
    Private Shared Function Get_UserProfile(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <userProfile>
                 </userProfile>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/oauth/identity", , "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlUserProfile(reader.ReadToEnd).<USER>.Elements)
            DelegateSub(FichierxmlFinal.ToString, UserName)
            Return True
        Catch ex As Exception
            DelegateSub("", UserName)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    ' -----------------Requetes pour la gestion des folders de la collection Discogs---------------
    Private Shared Function Get_CollectionFolderListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(UserName, "CollectionFolderlist")
        Dim xmlPartiel As XDocument
        Dim ReponseComplete As String = ""
        Try
            Dim FichierxmlFinal As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <COLLECTIONFOLDERLIST>
                     </COLLECTIONFOLDERLIST>

            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            Dim InfosReader = reader.ReadToEnd
            xmlPartiel = ConvertReponseXmlCollectionFolderList(InfosReader)
            Dim RequeteReleases = (From rel In xmlPartiel.<COLLECTIONFOLDERLIST>.<folders>
                                    Select rel)
            FichierxmlFinal.Root.Add(RequeteReleases)
            Thread.Sleep(1000)
            FichierxmlFinal.Save(PathNomFichierInfos)
            DelegateSub(PathNomFichierInfos, "")
            Return True
        Catch ex As Exception
            DelegateSub("", "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Add_CollectionFolder(ByVal UserName As String, ByVal FolderName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <folders>
                 </folders>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders", "name=" & FolderName, "POST")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlCollectionList(reader.ReadToEnd).<COLLECTIONLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, FolderName)
            Return True
        Catch ex As Exception
            DelegateSub("", FolderName)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Delete_CollectionFolder(ByVal UserName As String, ByVal FolderId As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim Element As XDocument = XDocument.Parse(FolderId)
        Try
            Dim InfoFolder As XDocument = XDocument.Parse(Get_CollectionFolder(UserName, Element.<folders>.<id>.Value, Nothing))
            If InfoFolder.<folders>.<count>.Value = "0" Then
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" & Element.<folders>.<id>.Value, , "DELETE")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                DelegateSub("", Element.<folders>.<id>.Value)
                Return True
            Else
                DelegateSub(FolderId, InfoFolder.<folders>.<count>.Value)
            End If
        Catch ex As Exception
            DelegateSub(FolderId, "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Get_CollectionFolder(ByVal UserName As String, ByVal FolderId As String, ByVal DelegateSub As DelegateRequestResult) As String
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <folders>
                 </folders>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" & FolderId, , "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlCollectionList(reader.ReadToEnd).<COLLECTIONLIST>.Elements)
            Return FichierxmlFinal.ToString
        Catch ex As Exception
            Return ""
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    ' -----------------Requetes pour la gestion de la COLLECTION Discogs---------------------------
    Private Shared Function Get_CollectionListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(UserName, "Collectionlist")
        Dim xmlPartiel As XDocument
        Dim ReponseComplete As String = ""
        Dim Folder As String = ExtraitChaine(UserName, "/", "")
        If Folder = "" Then Folder = "0" Else UserName = ExtraitChaine(UserName, "", "/")
        Try
            Dim FichierxmlFinal As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <COLLECTIONLIST>
                     </COLLECTIONLIST>

            Dim NumPage As Long = 0
            Dim TotalPage As Long
            Do
                NumPage += 1
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" & Folder & "/releases", "page=" & CStr(NumPage) & "&per_page=50", "GET")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                Dim InfosReader = reader.ReadToEnd
                xmlPartiel = ConvertReponseXmlCollectionList(InfosReader)
                TotalPage = CInt(xmlPartiel.<COLLECTIONLIST>.<pagination>.<pages>.Value)
                Dim RequeteReleases = (From rel In xmlPartiel.<COLLECTIONLIST>.<releases>
                                    Select rel)
                FichierxmlFinal.Root.Add(RequeteReleases)
                Thread.Sleep(1000)
            Loop While NumPage < TotalPage
            FichierxmlFinal.Save(PathNomFichierInfos)
            DelegateSub(PathNomFichierInfos, "")
            Return True
        Catch ex As Exception
            DelegateSub("", "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Add_CollectionId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim Folder As String = ExtraitChaine(UserName, "/", "")
        If Folder = "" Then Folder = "1" Else UserName = ExtraitChaine(UserName, "", "/")
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <releases>
                 </releases>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" & Folder & "/releases/" & idrelease, , "POST")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlCollectionList(reader.ReadToEnd).<COLLECTIONLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, idrelease)
            Return True
        Catch ex As Exception
            DelegateSub("", idrelease)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Delete_CollectionId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim Folder As String = ExtraitChaine(UserName, "/", "")
        If Folder = "" Then Folder = "1" Else UserName = ExtraitChaine(UserName, "", "/")
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" & Folder & "/releases/" & idrelease & "/instances/1", , "DELETE")
            hwebresponse.Close()
            DelegateSub(CStr(True), idrelease)
            Return True
        Catch ex As Exception
            If InStr(ex.Message, "404") > 0 Then
                DelegateSub(CStr(True), idrelease)
                Return True
            Else
                DelegateSub(CStr(False), idrelease)
                Return False
            End If
        End Try
    End Function
    Private Shared Function Move_CollectionId(ByVal UserName As String, ByVal idRelease As String, ByVal instance As String,
                                              ByVal idSourceFolder As String, ByVal idDestinationFolder As String,
                                              ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        If instance = "" Then instance = CStr(1)
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/collection/folders/" &
                                                        idSourceFolder & "/releases/" & idRelease & "/instances/" & instance,
                                                        "folder_id=" & idDestinationFolder, "POST")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            DelegateSub(idRelease & "/" & instance, idDestinationFolder)
            Return True
        Catch ex As Exception
            DelegateSub(idRelease & "/" & instance, idSourceFolder)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    ' -----------------Requetes pour la gestion de la WANTLIST Discogs-----------------------------
    Private Shared Function Get_WantListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(UserName, "Wantlist")
        Dim xmlPartiel As XDocument
        Dim ReponseComplete As String = ""
        Try
            Dim FichierxmlFinal As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <WANTLIST>
                     </WANTLIST>

            Dim NumPage As Long = 0
            Dim TotalPage As Long
            Do
                NumPage += 1
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/wants", "page=" & CStr(NumPage) & "&per_page=50", "GET")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                Dim InfosReader = reader.ReadToEnd
                xmlPartiel = ConvertReponseXmlWantList(InfosReader)
                TotalPage = CInt(xmlPartiel.<WANTLIST>.<pagination>.<pages>.Value)
                Dim RequeteReleases = (From rel In xmlPartiel.<WANTLIST>.<wants>
                                    Select rel)
                FichierxmlFinal.Root.Add(RequeteReleases)
                Thread.Sleep(1000)
            Loop While NumPage < TotalPage
            FichierxmlFinal.Save(PathNomFichierInfos)
            DelegateSub(PathNomFichierInfos, "")
            Return True
        Catch ex As Exception
            DelegateSub("", "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Get_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <wants>
                     <basic_information>
                     </basic_information>
                 </wants>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/releases/" & idrelease)
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root...<basic_information>.First.Add(ConvertReponseXmlWantList(reader.ReadToEnd).<WANTLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, idrelease)
            Return True
        Catch ex As Exception
            DelegateSub("", idrelease)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Add_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <wants>
                 </wants>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/wants/" & idrelease, , "PUT")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlWantList(reader.ReadToEnd).<WANTLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, idrelease)
            Return True
        Catch ex As Exception
            DelegateSub("", idrelease)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Delete_WantListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/wants/" & idrelease, , "DELETE")
            hwebresponse.Close()
            DelegateSub(CStr(True), idrelease)
            Return True
        Catch ex As Exception
            DelegateSub(CStr(False), idrelease)
            Debug.Print(ex.Message)
            If InStr(ex.Message, "404") > 0 Then
                DelegateSub(CStr(True), idrelease)
                Return True
            Else
                DelegateSub(CStr(False), idrelease)
                Return False
            End If
        End Try
    End Function
    ' -----------------Requetes pour la gestion de la SELLLIST Discogs-----------------------------
    Private Shared Function Get_SellListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(UserName, "SelllistTemp")
        Dim xmlPartiel As XDocument
        Dim ReponseComplete As String = ""
        Try
            Dim FichierxmlFinal As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <SELLLIST>
                     </SELLLIST>

            Dim NumPage As Long = 0
            Dim TotalPage As Long
            Do
                NumPage += 1
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/users/" & UserName & "/inventory", "page=" & CStr(NumPage) & "&per_page=50", "GET")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                Dim InfosReader = reader.ReadToEnd
                xmlPartiel = ConvertReponseXmlSellList(InfosReader)
                TotalPage = CInt(xmlPartiel.<SELLLIST>.<pagination>.<pages>.Value)
                Dim RequeteReleases = (From rel In xmlPartiel.<SELLLIST>.<listings>
                                    Select rel)
                FichierxmlFinal.Root.Add(RequeteReleases)
                Thread.Sleep(1000)
            Loop While NumPage < TotalPage
            FichierxmlFinal.Save(PathNomFichierInfos)
            DelegateSub(PathNomFichierInfos, "")
            Return True
        Catch ex As Exception
            DelegateSub("", "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Add_SellListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <listings>
                 </listings>
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/price_suggestions/" & idrelease, , "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            Dim Chaine = reader.ReadToEnd
            Chaine = ExtraitChaine(Chaine, "Very Good Plus (VG+)", "", Len("Very Good Plus (VG+)"))
            Chaine = ExtraitChaine(Chaine, "{", "}")
            Chaine = ExtraitChaine(Chaine, "value", "", Len("value"))
            Chaine = ExtraitChaine(Chaine, ":", "")
            Chaine = ExtraitChaine(Chaine, "", ".") & Left(ExtraitChaine(Chaine, ".", "", 0), 3)
            If Chaine = "" Then Chaine = "10"
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()

            Dim Input As String = "release_id=#N" & idrelease & "&" &
                                    "condition=Very Good Plus (VG+)&" &
                                    "price=#F" & Chaine & "&" &
                                    "status=Draft"
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/listings", Input, "POST")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.Add(ConvertReponseXmlSellList(reader.ReadToEnd).<SELLLIST>.Elements)

            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/listings/" &
                                                        FichierxmlFinal.<listings>.<listing_id>.Value, , "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.RemoveAll()
            FichierxmlFinal.Root.Add(ConvertReponseXmlSellList(reader.ReadToEnd).<SELLLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, idrelease)
            Return True
        Catch ex As Exception
            DelegateSub("", idrelease)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    Private Shared Function Delete_SellListId(ByVal UserName As String, ByVal idrelease As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Try
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/listings/" & idrelease, , "DELETE")
            hwebresponse.Close()
            DelegateSub(CStr(True), idrelease)
            Return True
        Catch ex As Exception
            DelegateSub(CStr(False), idrelease)
            Debug.Print(ex.Message)
            If InStr(ex.Message, "404") > 0 Then
                DelegateSub(CStr(True), idrelease)
                Return True
            Else
                DelegateSub(CStr(False), idrelease)
                Return False
            End If
        End Try
    End Function
    Private Shared Function Change_SellListId(ByVal Input As String, ByVal ListingId As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim FichierxmlFinal As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <listings>
                 </listings>
        Try
            Dim Id = ExtraitChaine(ListingId, "", "/")
            Dim IdRelease As String = ExtraitChaine(Input, "=", ")")
            If InStr(Input, "status=Draft") > 0 And InStr(ListingId, "price") = 0 Then
                If InStr(ListingId, "status") = 0 Then
                    Dim condition As String = ExtraitChaine(Input, "condition=", "&", Len("condition="))
                    hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/price_suggestions/" & IdRelease, , "GET")
                    reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                    Dim Chaine = reader.ReadToEnd
                    Chaine = ExtraitChaine(Chaine, condition, "", Len(condition))
                    Chaine = ExtraitChaine(Chaine, "{", "}")
                    Chaine = ExtraitChaine(Chaine, "value", "", Len("value"))
                    Chaine = ExtraitChaine(Chaine, ":", "")
                    Chaine = ExtraitChaine(Chaine, "", ".") & Left(ExtraitChaine(Chaine, ".", "", 0), 3)
                    If reader IsNot Nothing Then reader.Close()
                    If hwebresponse IsNot Nothing Then hwebresponse.Close()

                    Dim Debut As String = ExtraitChaine(Input, "", "&price")
                    Dim Fin As String = ExtraitChaine(Input, "&price", "", Len("&price"))
                    Fin = ExtraitChaine(Fin, "&", "", 0)
                    If Chaine <> "" Then Input = Debut & "&price=#F" & Chaine & Fin
                End If
            End If
            If InStr(Input, "status=Sold") = 0 Then
                Input = ExtraitChaine(Input, ")", "")
                hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/listings/" & Id, Input, "POST")
                reader = New IO.StreamReader(hwebresponse.GetResponseStream)
                If reader IsNot Nothing Then reader.Close()
                If hwebresponse IsNot Nothing Then hwebresponse.Close()
            End If
            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/listings/" & Id, , "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            FichierxmlFinal.Root.RemoveAll()
            FichierxmlFinal.Root.Add(ConvertReponseXmlSellList(reader.ReadToEnd).<SELLLIST>.Elements)
            DelegateSub(FichierxmlFinal.ToString, ListingId)
            Return True
        Catch ex As Exception
            DelegateSub("", ListingId)
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function
    ' -----------------Requetes pour la gestion de la ORDERS Discogs-----------------------------
    Private Shared Function Get_OrderListAll(ByVal UserName As String, ByVal DelegateSub As DelegateRequestResult) As Boolean
        Dim reader As IO.StreamReader = Nothing
        Dim hwebresponse As System.Net.HttpWebResponse = Nothing
        Dim PathNomFichierInfos As String = GetNomFichierXML(UserName, "OrdersList")
        Dim xmlPartiel As XDocument
        Dim ReponseComplete As String = ""
        Try
            Dim FichierxmlFinal As XDocument = _
                     <?xml version="1.0" encoding="utf-8"?>
                     <ORDERSLIST>
                     </ORDERSLIST>

            hwebresponse = oAuthDiscogs.WebRequestoAuth("http://" & Site & "/marketplace/orders", "per_page=50&sort=id&sort_order=desc", "GET")
            reader = New IO.StreamReader(hwebresponse.GetResponseStream)
            Dim InfosReader = reader.ReadToEnd
            xmlPartiel = ConvertReponseXmlOrdersList(InfosReader)
            Dim RequeteReleases = (From rel In xmlPartiel.<ORDERSLIST>.<orders>
                                Select rel)
            FichierxmlFinal.Root.Add(RequeteReleases)
            FichierxmlFinal.Save(PathNomFichierInfos)
            DelegateSub(PathNomFichierInfos, "")
            Return True
        Catch ex As Exception
            DelegateSub("", "")
        Finally
            If reader IsNot Nothing Then reader.Close()
            If hwebresponse IsNot Nothing Then hwebresponse.Close()
        End Try
        Return False
    End Function

    '***********************************************************************************************
    '--------------------------FUNCTION DE CONVERSION DE FICHIERS JSON VERS XML--------------------
    '***********************************************************************************************
    Private Shared Function ConvertReponseXmlUserProfile(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "USER" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function ConvertReponseXmlOrdersList(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "ORDERSLIST" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function ConvertReponseXmlSellList(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "SELLLIST" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function ConvertReponseXmlWantList(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "WANTLIST" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function ConvertReponseXmlCollectionList(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "COLLECTIONLIST" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function
    Private Shared Function ConvertReponseXmlCollectionFolderList(ByVal TexteJson As String) As XDocument
        Dim ChaineMiseEnForme As String = "{""" & "COLLECTIONFOLDERLIST" & """:" & TexteJson & "}"
        Return JsonConvert.DeserializeXNode(ChaineMiseEnForme)
    End Function

    '***********************************************************************************************
    '----------------------------------UTILITAIRES------------------------------------
    '***********************************************************************************************
    Private Shared Function GetNomFichierXML(ByVal user As String, ByVal typeRequete As String) As String
        Dim RepDest As String
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGEDATA
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        Dim NomFichier As String = String.Format("{0}\{1}{2}_{3}.xml", {RepDest, "Discogs", typeRequete, user})
        Return NomFichier
    End Function
End Class

