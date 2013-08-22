Option Compare Text
Imports System.Threading
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports mshtml
Imports System.Xml
Imports Microsoft.Win32
Imports System.Security.Permissions

Public Class WindowWebBrowser
    Implements iNotifyCollectionUpdate, iNotifyWantListUpdate

    '***********************************************************************************************
    '-----------------------------------DECLARATION POUR UTILISATION iWebBrowse2--------------------
    '***********************************************************************************************
    Shared ReadOnly SID_SWebBrowserApp As Guid = New Guid("0002DF05-0000-0000-C000-000000000046")
    <ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <Guid("6d5140c1-7436-11ce-8034-00aa006009fa")> _
    Friend Interface IServiceProvider
        Function QueryService(ByRef guidService As Guid, ByRef riid As Guid) As <MarshalAs(UnmanagedType.IUnknown)> Object
    End Interface

    '***********************************************************************************************
    '-----------------------------------MESSAGES DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Event UpdateSource(ByVal NouvelleSource As String)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String)
    Public Event IdCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionAdded
    Public Event IdCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionRemoved
    Public Event IdDiscogsCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionAdded
    Public Event IdDiscogsCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionRemoved
    Public Event IdWantlistAdded(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistAdded
    Public Event IdWantlistRemoved(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistRemoved
    Public Event IdWantlistUpdated(ByVal Document As XmlDocument, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistUpdated

    '***********************************************************************************************
    '-----------------------------------CONSTRUCTEUR DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        Try
            Dim regVersion As RegistryKey
            Dim keyValue As String = "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"
            Dim f As New RegistryPermission(RegistryPermissionAccess.Write, "HKEY_LOCAL_MACHINE\" & keyValue)
            regVersion = Registry.CurrentUser.OpenSubKey(keyValue, RegistryKeyPermissionCheck.Default,
                                                         Security.AccessControl.RegistryRights.WriteKey Or Security.AccessControl.RegistryRights.ReadKey)
            If regVersion Is Nothing Then
                regVersion = Registry.CurrentUser.CreateSubKey(keyValue, RegistryKeyPermissionCheck.Default)
            End If
            If regVersion IsNot Nothing Then
                Dim Valeur = regVersion.GetValue("GBPlayer.exe")
                If Valeur IsNot Nothing Then
                    'MsgBox(regVersion.GetValue("GBPlayer.exe").ToString)
                Else
                    regVersion.SetValue("GBPlayer.exe", 10000, RegistryValueKind.DWord)
                End If
            End If
        Catch ex As Exception
            Debug.Print("erreur de registre")
        End Try
    End Sub
    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    '***********************************************************************************************
    '-----------------------------------PROCEDURE DE FERMETURE DE LA FENETRE------------------------
    '***********************************************************************************************
    Public Sub CloseWebBrowser(ByRef Config As ConfigPerso)
        ' If Config IsNot Nothing Then Config.PlayerPosition = New Point(Me.Left, Me.Top)
        Me.Close()
    End Sub

    '***********************************************************************************************
    '----------------------------PROCEDURE DE MISE A JOUR DE LA FENETRE EN ASYNCHRONE---------------
    '***********************************************************************************************
    Private Delegate Sub WebRequestDelegate(ByVal NewUrl As Uri)
    Public Sub UpdateUrl(ByVal NewUrl As Uri)
        Dim ReadTag As New WebRequestDelegate(AddressOf WebRequest)
        ReadTag.BeginInvoke(NewUrl, Nothing, Nothing)
    End Sub
    Private Sub WebRequest(ByVal NewUrl As Uri)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           Navigateur.Source = NewUrl
                                                           If Not (IsVisible) Then
                                                               Show()
                                                           Else
                                                               Me.WindowState = Windows.WindowState.Normal
                                                               ' Me.Focus()
                                                           End If
                                                       End Sub))
    End Sub

    '***********************************************************************************************
    '----------------------------GESTION DES MESSAGES DU BROWSER------------------------------------
    '***********************************************************************************************
    Private iWebBrowser2Init As Boolean
    'REPONSE MESSAGE EN FIN DE NAVIGATION DE LA PAGE
    Private Sub Navigateur_Navigated(ByVal sender As Object, ByVal e As System.Windows.Navigation.NavigationEventArgs) Handles Navigateur.Navigated
        If Not iWebBrowser2Init Then
            BoutonBack.IsEnabled = False
            BoutonNext.IsEnabled = False
            Dim WebBrowserActif As SHDocVw.IWebBrowser2 = CType(Navigateur.Document, IServiceProvider).QueryService(SID_SWebBrowserApp, GetType(SHDocVw.IWebBrowser2).GUID)
            WebBrowserActif.Silent = True
            AddHandler CType(WebBrowserActif, SHDocVw.DWebBrowserEvents_Event).DownloadComplete, New SHDocVw.DWebBrowserEvents_DownloadCompleteEventHandler(AddressOf DownloadComplete)
            AddHandler CType(WebBrowserActif, SHDocVw.DWebBrowserEvents_Event).ProgressChange, New SHDocVw.DWebBrowserEvents_ProgressChangeEventHandler(AddressOf ProgressChange)
            ' HideScriptErrors(Navigateur, True)
            iWebBrowser2Init = True
        End If
    End Sub
    'REPONSE AU MESSAGE IWebBrowser2 DE PROGRESSION DU CHARGEMENT
    Private Sub ProgressChange(ByVal Value As Integer, ByVal MaxValue As Integer)
        Progression.Maximum = MaxValue
        Progression.Value = Value
    End Sub

    '***********************************************************************************************
    '---------------GESTION INTERCEPTION DES BOUTONS AJOUTER ET SUPPRIMER DE LA PAGE RELEASE--------
    '***********************************************************************************************
    'REPONSE AU MESSAGE IWebBrowser2 CHARGEMENT COMPLET 
    Dim BoutonAjoutCollection As IHTMLButtonElement
    Dim BoutonAjoutWantList As IHTMLButtonElement
    Dim BoutonSuppressionCollection As IHTMLAnchorElement
    Dim BoutonSuppressionWantList As IHTMLAnchorElement
    Private Sub DownloadComplete()
        'INTERCEPTION DES MESSAGES DE LA PAGE RELEASE
        If Navigateur.CanGoBack Then BoutonBack.IsEnabled = True Else BoutonBack.IsEnabled = False
        If Navigateur.CanGoForward Then BoutonNext.IsEnabled = True Else BoutonNext.IsEnabled = False

        Progression.Value = 0
        If InStr(Navigateur.Source.OriginalString, "/release/") > 0 Then
            If BoutonAjoutCollection IsNot Nothing Then
                RemoveHandler CType(BoutonAjoutCollection, HTMLButtonElementEvents_Event).onclick, New HTMLButtonElementEvents_onclickEventHandler(AddressOf OnClickAddCollection)
                BoutonAjoutCollection = Nothing
            End If
            If BoutonAjoutWantList IsNot Nothing Then
                RemoveHandler CType(BoutonAjoutWantList, HTMLButtonElementEvents_Event).onclick, New HTMLButtonElementEvents_onclickEventHandler(AddressOf OnClickAddWantList)
                BoutonAjoutWantList = Nothing
            End If
            Dim doc As HTMLDocument = CType(Navigateur.Document, IHTMLDocument)
            Debug.Print("COLLECTION", "Creation bouton coll")
            Try
                For Each i As IHTMLElement In doc.all
                    Select Case i.nodename
                        Case "BUTTON"
                            If i.className IsNot Nothing Then
                                If InStr(i.className, "coll_add_button") > 0 Then
                                    BoutonAjoutCollection = CType(i, IHTMLButtonElement)
                                    AddHandler CType(BoutonAjoutCollection, HTMLButtonElementEvents_Event).onclick, New HTMLButtonElementEvents_onclickEventHandler(AddressOf OnClickAddCollection)
                                    Debug.Print("COLLECTION", "Creation bouton coll")
                                End If
                                If InStr(i.className, "want_add_button") > 0 Then
                                    BoutonAjoutWantList = CType(i, IHTMLButtonElement)
                                    AddHandler CType(BoutonAjoutWantList, HTMLButtonElementEvents_Event).onclick, New HTMLButtonElementEvents_onclickEventHandler(AddressOf OnClickAddWantList)
                                    Debug.Print("COLLECTION", "Creation bouton want")
                                End If
                            End If
                        Case "DIV"
                            If i.className IsNot Nothing Then
                                If InStr(i.className, "collection") > 0 Then
                                    For Each j As IHTMLElement In i.all
                                        If j.nodename = "A" Then
                                            If BoutonSuppressionCollection IsNot Nothing Then
                                                RemoveHandler CType(BoutonSuppressionCollection, HTMLAnchorEvents_Event).onclick, New HTMLAnchorEvents_onclickEventHandler(AddressOf OnClickRemoveCollection)
                                                BoutonSuppressionCollection = Nothing
                                            End If
                                            BoutonSuppressionCollection = CType(j, IHTMLAnchorElement)
                                            AddHandler CType(BoutonSuppressionCollection, HTMLAnchorEvents_Event).onclick, New HTMLAnchorEvents_onclickEventHandler(AddressOf OnClickRemoveCollection)
                                            Debug.Print("COLLECTION", "Creation bouton sup coll")
                                        End If
                                    Next
                                End If
                            End If
                            If i.className IsNot Nothing Then
                                If InStr(i.className, "wantlist") > 0 Then
                                    For Each j As IHTMLElement In i.all
                                        If j.nodename = "A" Then
                                            If BoutonSuppressionWantList IsNot Nothing Then
                                                RemoveHandler CType(BoutonSuppressionWantList, HTMLAnchorEvents_Event).onclick, New HTMLAnchorEvents_onclickEventHandler(AddressOf OnClickRemoveWantList)
                                                BoutonSuppressionWantList = Nothing
                                            End If
                                            BoutonSuppressionWantList = CType(j, IHTMLAnchorElement)
                                            AddHandler CType(BoutonSuppressionWantList, HTMLAnchorEvents_Event).onclick, New HTMLAnchorEvents_onclickEventHandler(AddressOf OnClickRemoveWantList)
                                            Debug.Print("COLLECTION", "Creation bouton sup want")
                                        End If
                                    Next
                                End If
                            End If
                    End Select
                Next
            Catch ex As Exception
                Debug.Print("erreur")
            End Try
        End If
    End Sub
    'REPONSE A L'APPUI SUR LES BOUTONS DE LA PAGE RELEASE
    Dim WithEvents TimerUpdate As New Timers.Timer(1000)
    Private Function OnClickAddCollection() As Boolean
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        RaiseEvent IdDiscogsCollectionAdded(ChaineId, Me)
        TimerUpdate.AutoReset = False
        TimerUpdate.Enabled = True
        Return False
    End Function
    Private Function OnClickAddWantList() As Boolean
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        RaiseEvent IdWantlistAdded(ChaineId, Me)
        TimerUpdate.AutoReset = False
        TimerUpdate.Enabled = True
        Return False
    End Function
    Private Function OnClickRemoveCollection() As Boolean
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        RaiseEvent IdDiscogsCollectionRemoved(ChaineId, Me)
        TimerUpdate.AutoReset = False
        TimerUpdate.Enabled = True
        Return False
    End Function
    Private Function OnClickRemoveWantList() As Boolean
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        RaiseEvent IdWantlistRemoved(ChaineId, Me)
        TimerUpdate.AutoReset = False
        TimerUpdate.Enabled = True
        Return False
    End Function
    'UPDATE APPELE APRES AJOUT D'UN ELEMENT POUR TENIR A JOUR LA LISTE DES BOUTONS DE LA PAGE
    Private Delegate Sub NoArgDelegate()
    Private Sub TimerUpdate_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerUpdate.Elapsed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           DownloadComplete()
                                                       End Sub))
    End Sub

    '***********************************************************************************************
    '---------------GESTION DES NOTIFICATIONS DE MISE A JOUR COLLECTION ET WANTLIST-----------------
    '***********************************************************************************************
    Public Function NotifyAddIdCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdCollection
    End Function
    Public Function NotifyRemoveIdCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdCollection
    End Function
    Public Function NotifyAddIdDiscogsCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdDiscogsCollection
        If InStr(Navigateur.Source.OriginalString, "/release/" & Id) > 0 Then
            Navigateur.Refresh()
            ' UpdateUrl(New Uri(Navigateur.Source.OriginalString))
        End If
        Return True
    End Function
    Public Function NotifyRemoveIdDiscogsCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdDiscogsCollection
        If InStr(Navigateur.Source.OriginalString, "/release/" & id) > 0 Then
            Navigateur.Refresh()
            'UpdateUrl(New Uri(Navigateur.Source.OriginalString))
        End If
        Return True
    End Function
    Public Function NotifyAddIdWantlist(ByVal Id As String) As Boolean Implements iNotifyWantListUpdate.NotifyAddIdWantlist
        If InStr(Navigateur.Source.OriginalString, "/release/" & Id) > 0 Then
            Navigateur.Refresh()
            'UpdateUrl(New Uri(Navigateur.Source.OriginalString))
        End If
        Return True
    End Function
    Public Function NotifyRemoveIdWantlist(ByVal id As String) As Boolean Implements iNotifyWantListUpdate.NotifyRemoveIdWantlist
        If InStr(Navigateur.Source.OriginalString, "/release/" & id) > 0 Then
            Navigateur.Refresh()
            'UpdateUrl(New Uri(Navigateur.Source.OriginalString))
        End If
        Return True
    End Function
    Public Function NotifyUpdateWantList(ByVal Document As XmlDocument) As Boolean Implements iNotifyWantListUpdate.NotifyUpdateWantList

    End Function

    '***********************************************************************************************
    '-----------------------------------MISE A JOUR DE L'INTERFACE DE LA FENETRE--------------------
    '***********************************************************************************************
    ' MESSAGE EN FIN DE MISE A JOUR PAGE
    Private Sub Navigateur_Navigating(ByVal sender As Object, ByVal e As System.Windows.Navigation.NavigatingCancelEventArgs) Handles Navigateur.Navigating
        CheminPage.Text = e.Uri.ToString
    End Sub
    ' BOUTON PAGE PRECEDENTE ACTIVE
    Private Sub BoutonBack_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BoutonBack.Click
        If Navigateur.CanGoBack Then
            Navigateur.GoBack()
        End If
    End Sub
    ' BOUTON PAGE SUIVANTE ACTIVE
    Private Sub BoutonNext_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BoutonNext.Click
        If Navigateur.CanGoForward Then
            Navigateur.GoForward()
        End If
    End Sub
    ' MISE A JOUR FENETRE SUIVANT LIEN
    Private Sub CheminPage_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles CheminPage.KeyDown
        Select Case e.Key
            Case Key.Enter
                Dim NewUri As Uri = New Uri(CheminPage.Text)
                Navigateur.Navigate(NewUri)
        End Select
    End Sub
    ' BOUTON TRANSFERT DE LA PAGE VERS LA PAGE PRINCIPALE
    Private Sub BoutonTransfert_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BoutonTransfert.Click
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        If Navigateur.Source IsNot Nothing Then RaiseEvent UpdateSource(ChaineId)
    End Sub
    ' BOUTON TRANSFERT DES DONNEES DANS DOCUMENT XML POUR EXPORT VERS EXCEL
    Private Sub ExportInfosDiscogs_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles ExportInfosDiscogs.Click
        Dim ChaineId As String = ExtraitChaine(ExtraitChaine(Navigateur.Source.OriginalString, "/release/", "", 9), "", "?", , True)
        Dim NewDiscogs As Discogs = New Discogs(ChaineId)
        If Navigateur.Source IsNot Nothing Then NewDiscogs.ExportXmlRelease(Discogs.Get_ReleaseId(Navigateur.Source.ToString))
    End Sub
    ' BOUTON DE LANCEMENT RECHERCHE ID
    Private Sub BoutonRechercheId_Click(sender As Object, e As RoutedEventArgs) Handles BoutonRechercheId.Click
        Dim doc As HTMLDocument = CType(Navigateur.Document, IHTMLDocument)
        Dim Chaine As String = doc.selection.ToString
        If (doc.selection.type = "text") Then
            Dim Zone As IHTMLTxtRange = doc.selection.createRange
            Chaine = Zone.text
            RaiseEvent RequeteRecherche(Chaine)
            Exit Sub
        End If
        If InStr(CheminPage.Text, "/release/") > 0 Then
            Dim Chaine2 As String = ExtraitChaine(CheminPage.Text, "/release/", "", 9)
            Chaine = ExtraitChaine(CheminPage.Text, ".com/", "/", 5)
            If Chaine = "release" Then
                RaiseEvent RequeteRecherche("id:" & Chaine2)
            Else
                RaiseEvent RequeteRecherche(RemplaceOccurences(Chaine, "-", " "))
            End If
        ElseIf InStr(CheminPage.Text, "/master/") > 0 Then
            Chaine = ExtraitChaine(CheminPage.Text, ".com/", "/", 5)
            If Chaine <> "" Then
                RaiseEvent RequeteRecherche(RemplaceOccurences(Chaine, "-", " "))
            End If
        ElseIf InStr(CheminPage.Text, "/artist/") > 0 Then
            Chaine = ExtraitChaine(CheminPage.Text, "/artist/", "", 8)
            If Chaine <> "" Then
                RaiseEvent RequeteRecherche("artiste:" & RemplaceOccurences(Chaine, "+", " "))
            End If
        End If
    End Sub


    'Procedure permettant de masquer les fenetres d'erreur de script.
    '   Private Sub HideScriptErrors(ByVal wb As WebBrowser, ByVal Hide As Boolean)
    '   Debug.Print(Navigateur.Document.GetType.ToString)
    '   Dim fiComWebBrowser As FieldInfo = GetType(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance Or BindingFlags.NonPublic)
    '   If fiComWebBrowser Is Nothing Then Return
    '   Dim objComWebBrowser As Object = fiComWebBrowser.GetValue(wb)
    '   If objComWebBrowser Is Nothing Then Return
    '   objComWebBrowser.[GetType]().InvokeMember("Silent", BindingFlags.SetProperty, Nothing, objComWebBrowser, New Object() {Hide})
    'End Sub

End Class
