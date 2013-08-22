Imports System.Threading
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports mshtml
Imports gbDev.OAuthLib

Public Class WindowConfig
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

    '***********************************************************************************************
    '-----------------------------------CONSTRUCTEUR DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        If File.Exists(ConfigPerso.GetConfigPersoPath) Then DataProvider.Source = New Uri(ConfigPerso.GetConfigPersoPath)
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
    Private Delegate Sub NoArgDelegate()
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
            Dim WebBrowserActif As SHDocVw.IWebBrowser2 = CType(Navigateur.Document, IServiceProvider).QueryService(SID_SWebBrowserApp, GetType(SHDocVw.IWebBrowser2).GUID)
            WebBrowserActif.Silent = True
            AddHandler CType(WebBrowserActif, SHDocVw.DWebBrowserEvents_Event).DownloadComplete, New SHDocVw.DWebBrowserEvents_DownloadCompleteEventHandler(AddressOf DownloadComplete)
            AddHandler CType(WebBrowserActif, SHDocVw.DWebBrowserEvents_Event).ProgressChange, New SHDocVw.DWebBrowserEvents_ProgressChangeEventHandler(AddressOf ProgressChange)
            iWebBrowser2Init = True
        End If
    End Sub
    'REPONSE AU MESSAGE IWebBrowser2 DE PROGRESSION DU CHARGEMENT
    Private Sub ProgressChange(ByVal Value As Integer, ByVal MaxValue As Integer)
        Debug.Print(Value & " - " & MaxValue)
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
        If Navigateur.Source IsNot Nothing Then
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
                                    If InStr(i.className, "coll_block") > 0 Then
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
                                    If InStr(i.className, "want_block") > 0 Then
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
        End If
    End Sub
    'REPONSE A L'APPUI SUR LES BOUTONS DE LA PAGE RELEASE
    Dim WithEvents TimerUpdate As New Timers.Timer(1000)
    Private Function OnClickAddCollection() As Boolean
    End Function
    Private Function OnClickAddWantList() As Boolean
    End Function
    Private Function OnClickRemoveCollection() As Boolean
    End Function
    Private Function OnClickRemoveWantList() As Boolean
    End Function

    Dim UserData As OAuthUser = Nothing
    Dim AccessTokenData As oAuthAccessToken
    Private Sub AccessToken_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If DataProvider.Source IsNot Nothing Then
            tagconsumerKey.Text = Trim(tagconsumerKey.Text)
            tagconsumerSecret.Text = Trim(tagconsumerSecret.Text)
            tagconsumerKey.IsEnabled = False
            tagconsumerSecret.IsEnabled = False
            AccessToken.IsEnabled = False
            Dim source As String = DataProvider.Source.LocalPath
            DataProvider.Document.Save(source)
            If Not oAuthDiscogs.TestoAuth(tagconsumerKey.Text, tagconsumerSecret.Text,
                                           tagtokenValue.Text, tagtokenSecret.Text) Then
                reqToken.IsEnabled = True
                tagCodeDiscogs.IsEnabled = True
                Dim NewUri As Uri = oAuthDiscogs.GetUriAutorization()
                If NewUri IsNot Nothing Then
                    DockConsumerKey.Visibility = Windows.Visibility.Collapsed
                    DockConsumerSecret.Visibility = Windows.Visibility.Collapsed
                    DockCodeValidation.Visibility = Windows.Visibility.Visible
                    AccessToken.Visibility = Windows.Visibility.Collapsed
                    reqToken.Visibility = Windows.Visibility.Visible
                    UpdateUrl(NewUri)
                Else
                    wpfMsgBox.MsgBoxInfo("Erreur d'authentification Discogs",
                                         "Les clés entrées ne sont pas valides pour oAuth Discogs", , "Echec de la procédure oAuth de Discogs")
                    Close()
                End If
            Else
                Close()
            End If
        End If
    End Sub
    Private Sub reqToken_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim Chaine = Trim(tagCodeDiscogs.Text)
        Try
            If Chaine <> "" Then
                AccessTokenData = oAuthDiscogs.GetAccessAutorization(Chaine)
                tagtokenValue.Text = AccessTokenData.TokenValue
                tagtokenSecret.Text = AccessTokenData.TokenSecret
                Dim source As String = DataProvider.Source.LocalPath
                DataProvider.Document.Save(source)
            End If
            Close()
        Catch ex As Exception
            wpfMsgBox.MsgBoxInfo("Erreur d'authentification Discogs",
                                 "Le code de validation n'est pas valide. Recommencer la procédure à partir des options de configuration", , "Echec de la procédure oAuth de Discogs")
        End Try
        Close()
    End Sub

End Class
