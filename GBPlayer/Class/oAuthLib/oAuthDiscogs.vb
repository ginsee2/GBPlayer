Imports System.Collections.Generic
Imports System.Text
Imports System.Diagnostics
Imports System.Net
Imports gbDev.OAuthLib
Imports System.Threading


Class oAuthDiscogs
    Shared Event ConsumerKeyRequest(ByVal SettingUri As Uri)
    Shared Event StateChanged()


    Shared Property AccessToken As oAuthAccessToken
    Shared Property Utilisateur As OAuthUser
    Shared Property reqToken As OAuthRequestToken
    ' Shared Property AuthentificationIsOk As Boolean
    Shared Property oAuthNotAvailable As Boolean
    Shared Property DiscogsNotConnected As Boolean
    Shared Property ProxyCancelRequest As Boolean
    Shared Property DiscogsoAuthCancelled As Boolean

    Private Shared Function GetDiscogsSettingUri() As Uri
        Return New Uri("https://www.discogs.com/settings/applications")
    End Function
    Public Shared Function TestoAuth(ByVal tagconsumerKey As String, ByVal tagconsumerSecret As String,
                              ByVal tagtokenValue As String, ByVal tagtokenSecret As String) As Boolean
        Utilisateur = New OAuthUser(tagconsumerKey, tagconsumerSecret)
        AccessToken = New oAuthAccessToken(tagtokenValue, tagtokenSecret)
        Try
            If Utilisateur.AccessProtectedResource(AccessToken,
                                                     "http://api.discogs.com/oauth/identity",
                                                     "GET",
                                                     "discogs",
                                                     Nothing).StatusCode.ToString = "OK" Then
                oAuthNotAvailable = False
                DiscogsNotConnected = False
                ProxyCancelRequest = False
                RaiseEvent StateChanged()
                Return True
            Else
                AccessToken = Nothing
            End If
        Catch ex As Exception
            AccessToken = Nothing
        End Try
        Return False
    End Function
    Public Shared Function GetUriAutorization() As Uri
        Try
            reqToken = Utilisateur.ObtainUnauthorizedRequestToken("http://api.discogs.com/oauth/request_token")
            Return New Uri(OAuthUser.BuildUserAuthorizationURL("http://www.discogs.com/oauth/authorize", reqToken))
        Catch ex As Exception
            AccessToken = Nothing
            Return Nothing
        End Try
    End Function
    Public Shared Function GetAccessAutorization(ByVal chaine As String) As oAuthAccessToken
        Try
            AccessToken = Utilisateur.RequestAccessToken(chaine, reqToken, "http://api.discogs.com/oauth/access_token")
            oAuthNotAvailable = False
            DiscogsNotConnected = False
            ProxyCancelRequest = False
            Return AccessToken
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Shared Sub GetDiscogsoAuthAuthentification()
        Try
            Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
            ConfigUtilisateur = ConfigPerso.LoadConfig
            Utilisateur = New OAuthUser(ConfigUtilisateur.DISCOGSCONNECTION_consumerKey,
                                ConfigUtilisateur.DISCOGSCONNECTION_consumerSecret)
            AccessToken = New oAuthAccessToken(ConfigUtilisateur.DISCOGSCONNECTION_tokenValue,
                                               ConfigUtilisateur.DISCOGSCONNECTION_tokenSecret)
            If Utilisateur.AccessProtectedResource(AccessToken,
                                                         "http://api.discogs.com/oauth/identity",
                                                         "GET",
                                                         "discogs",
                                                         Nothing).StatusCode.ToString = "OK" Then
                oAuthNotAvailable = False
                DiscogsNotConnected = False
                ProxyCancelRequest = False
                RaiseEvent StateChanged()
            Else
                AccessToken = Nothing
            End If
        Catch ex As WebException
            AccessToken = Nothing
            If ex.Response IsNot Nothing Then
                If CType(ex.Response, HttpWebResponse).StatusCode = 407 Then
                    If Not ProxyCancelRequest Then
                        wpfMsgBox.MsgBoxInfo("Erreur de connection au site Discogs", ex.Message, , "Echec de la procédure oAuth de Discogs")
                        ProxyCancelRequest = True
                    End If
                    ex.Response.Close()
                    Exit Sub
                End If
            Else
            End If
            Select Case ex.Status
                Case WebExceptionStatus.ProtocolError
                    If Not oAuthNotAvailable Then
                        oAuthNotAvailable = True
                        RaiseEvent ConsumerKeyRequest(GetDiscogsSettingUri)
                    End If
                Case WebExceptionStatus.NameResolutionFailure
                    If Not DiscogsNotConnected Then
                        wpfMsgBox.MsgBoxInfo("Erreur de connection au site Discogs", "Vérifier si une connection internet est valide sur le poste", , "Echec de la procédure oAuth de Discogs")
                        DiscogsNotConnected = True
                    End If
                Case Else
                    wpfMsgBox.MsgBoxInfo("Erreur de connection au site Discogs", ex.Message, , "Echec de la procédure oAuth de Discogs")
                    DiscogsoAuthCancelled = True
             End Select
            Exit Sub
        End Try
    End Sub
    Public Shared Function WebRequestoAuth(ByVal urlRequete As String, Optional ByVal ParametresRequete As String = "", Optional ByVal methode As String = "GET") As System.Net.HttpWebResponse
        If DiscogsoAuthCancelled Then Return Nothing
        If (Utilisateur Is Nothing) Or (AccessToken Is Nothing) Then GetDiscogsoAuthAuthentification()
        '   If Not AuthentificationIsOk Then Return Nothing
        If ProxyCancelRequest Or DiscogsNotConnected Or oAuthNotAvailable Then Return Nothing
        If (Utilisateur IsNot Nothing) And (AccessToken IsNot Nothing) Then
            Return Utilisateur.AccessProtectedResource(AccessToken,
                                                         urlRequete,
                                                         methode,
                                                         "discogs",
                                                         OAuthLib.oAuthParameter.Parse(ParametresRequete))
        Else
            Return Nothing
        End If
    End Function
    Public Shared Sub SaveConfiguration(ByVal ConfigUtilisateur As gbDev.ConfigPerso)
        If Utilisateur IsNot Nothing Then
            If Utilisateur._consumerKey = "" Then Exit Sub
            ConfigUtilisateur.DISCOGSCONNECTION_consumerKey = Utilisateur._consumerKey
            ConfigUtilisateur.DISCOGSCONNECTION_consumerSecret = Utilisateur._consumerSecret
        End If
        If AccessToken IsNot Nothing Then
            ConfigUtilisateur.DISCOGSCONNECTION_tokenValue = AccessToken.TokenValue
            ConfigUtilisateur.DISCOGSCONNECTION_tokenSecret = AccessToken.TokenSecret
        End If
    End Sub
End Class

