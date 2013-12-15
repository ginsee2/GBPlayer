Imports System.Threading
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports mshtml
Imports gbDev.OAuthLib
Imports gbDev.DiscogsServer
Imports System.Xml

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
    End Sub
    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    '***********************************************************************************************
    '-----------------------------------PROCEDURE DE FERMETURE DE LA FENETRE------------------------
    '***********************************************************************************************
    Public Sub CloseWebBrowser()
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

    Private AccessTokenData As oAuthAccessToken
    Private Sub DownloadComplete()
        'INTERCEPTION DES MESSAGES DE LA PAGE RELEASE
        If Navigateur.Source IsNot Nothing Then
            If InStr(Navigateur.Source.OriginalString, "?oauth_token") > 0 Then
                Dim doc As HTMLDocument = CType(Navigateur.Document, IHTMLDocument)
                Try
                    For Each i As IHTMLElement In doc.all
                        Select Case i.nodename
                            Case "DIV"
                                If i.id IsNot Nothing Then
                                    If InStr(i.id, "page") > 0 Then
                                        For Each j As IHTMLElement In i.all
                                            If j.className = "auth_success_verify_code" Then
                                                FinaliseDemandeAcces(j.outerText)
                                                Exit Sub
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
    Public Sub DemandeAcces()
        If Not oAuthDiscogs.TestoAuth(Application.Config.discogsConnection_consumerKey, Application.Config.discogsConnection_consumerSecret,
                                       Application.Config.discogsConnection_tokenValue, Application.Config.discogsConnection_tokenSecret) Then
            Dim NewUri As Uri = oAuthDiscogs.GetUriAutorization()
            If NewUri IsNot Nothing Then
                UpdateUrl(NewUri)
            Else
                wpfMsgBox.MsgBoxInfo("Erreur d'authentification Discogs",
                                     "Les clés entrées ne sont pas valides pour oAuth Discogs", , "Echec de la procédure oAuth de Discogs")
                Close()
            End If
        Else
            DiscogsServer.RequestGet_UserProfile("", New DelegateRequestResult(AddressOf DiscogsServerGetUserProfile))
            Close()
        End If


    End Sub
    Private Sub FinaliseDemandeAcces(CodeValidationDiscogs As String)
        Dim Chaine = Trim(CodeValidationDiscogs)
        Try
            If Chaine <> "" Then
                AccessTokenData = oAuthDiscogs.GetAccessAutorization(Chaine)
                DiscogsServer.RequestGet_UserProfile("", New DelegateRequestResult(AddressOf DiscogsServerGetUserProfile))
            End If
            Close()
        Catch ex As Exception
            wpfMsgBox.MsgBoxInfo("Erreur d'authentification Discogs",
                                 "Le code de validation n'est pas valide. Recommencer la procédure à partir des options de configuration", , "Echec de la procédure oAuth de Discogs")
        End Try
        Close()
    End Sub
    Private Sub DiscogsServerGetUserProfile(ByVal UserProfile As String, ByVal UserName As String)
        If UserProfile <> "" Then
            Dim doc As New XmlDocument()
            doc.LoadXml(UserProfile)
            UserName = doc.SelectSingleNode("userProfile/username").InnerText
            'Dim guid As String = GetProfileGUID()
            'Dim userID As String = Application.Config.user_id
            'Dim DicRetour = FreeServer.TestValidUser("", guid, userID)
            'If DicRetour.ContainsKey("UserID") Then
            ' Application.Config.discogsConnection_tokenValue = AccessTokenData.TokenValue
            'Application.Config.discogsConnection_tokenSecret = AccessTokenData.TokenSecret
            'Application.Config.user_name = UserName
            'Application.Config.user_id = DicRetour.Item("UserID")
            'Return
            'Else
            'userID = FreeServer.Inscription("", Guid)
            'DicRetour = FreeServer.TestValidUser(UserName, Guid, userID)
            'If DicRetour.ContainsKey("UserID") Then
            Application.Config.discogsConnection_tokenValue = AccessTokenData.TokenValue
            Application.Config.discogsConnection_tokenSecret = AccessTokenData.TokenSecret
            Application.Config.user_name = UserName
            ' Application.Config.user_id = DicRetour.Item("UserID")
            ' Return
            'End If
            'End If
        Else
            wpfMsgBox.MsgBoxInfo("Erreur d'authentification Discogs",
                                 "Le nom utilisateur non valide. Recommencer la procédure à partir des options de configuration", , "Echec de la procédure oAuth de Discogs")
        End If
    End Sub

End Class
