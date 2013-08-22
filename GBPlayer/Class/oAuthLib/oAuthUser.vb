Imports System.Collections.Generic
Imports System.Text
Imports System.Net
Imports System.Security.Cryptography
Imports System.IO
Imports System.Threading

Namespace OAuthLib
    Public Class OAuthUser
        Const GBAU_UserAgent = "GBPLayer3.0 2012 VB.net / Application Discogs API v2"
        Public ReadOnly _consumerKey As String
        Public ReadOnly _consumerSecret As String

        Public Property Proxy() As WebProxy
        Public Property responseParameters As oAuthParameter()

        Public Sub New(ByVal consumerKey As String, ByVal consumerSecret As String)
            _consumerKey = consumerKey
            _consumerSecret = consumerSecret
        End Sub

        Public Function ObtainUnauthorizedRequestToken(ByVal requestTokenUrl As String) As OAuthRequestToken
            Dim oauth_consumer_key As String = _consumerKey
            Dim oauth_signature_method As String = "HMAC-SHA1"
            Dim oauth_timestamp As String = ((DateTime.UtcNow.Ticks - New DateTime(1970, 1, 1).Ticks) \ (1000 * 10000)).ToString()
            Dim oauth_nonce As String = Guid.NewGuid().ToString()
            Dim oauth_callback As String = "oob"

            Dim oauth_signature As String = CreateHMACSHA1Signature(
                                   "POST",
                                   requestTokenUrl,
                                   oAuthParameter.ConCatAsArray(New oAuthParameter() {
                                                      New oAuthParameter("oauth_consumer_key", oauth_consumer_key),
                                                      New oAuthParameter("oauth_signature_method", oauth_signature_method),
                                                      New oAuthParameter("oauth_timestamp", oauth_timestamp),
                                                      New oAuthParameter("oauth_nonce", oauth_nonce),
                                                      New oAuthParameter("oauth_callback", oauth_callback)}),
                                  _consumerSecret,
                                  "")

            Dim req As HttpWebRequest = System.Net.WebRequest.Create(requestTokenUrl)

            Dim data As String = "oauth_consumer_key=" & oAuthParameter.EncodeParameterString(oauth_consumer_key) & "&" &
                   "oauth_signature_method=" & oAuthParameter.EncodeParameterString(oauth_signature_method) & "&" &
                  "oauth_signature=" & oAuthParameter.EncodeParameterString(oauth_signature) & "&" &
                 "oauth_timestamp=" & oAuthParameter.EncodeParameterString(oauth_timestamp) & "&" &
                "oauth_nonce=" & oAuthParameter.EncodeParameterString(oauth_nonce) & "&" &
               "oauth_callback=" & oAuthParameter.EncodeParameterString(oauth_callback)

            If Proxy IsNot Nothing Then req.Proxy = Proxy
            req.Method = "POST"
            req.ServicePoint.Expect100Continue = False
            req.UserAgent = GBAU_UserAgent
            req.ContentType = "application/x-www-form-urlencoded"
            req.Headers.Add("Accept-Encoding", "gzip")
            req.AutomaticDecompression = Net.DecompressionMethods.GZip
            req.Timeout = 10000
            Dim encoding As New ASCIIEncoding
            Dim TabBytes As Byte() = encoding.GetBytes(data)
            req.ContentLength = TabBytes.Length
            Dim Writer As Stream = req.GetRequestStream
            Writer.Write(TabBytes, 0, TabBytes.Length)
            Writer.Close()
            Dim resp As HttpWebResponse = Nothing
            Try
                resp = DirectCast(req.GetResponse(), HttpWebResponse)
                Dim sr As New StreamReader(resp.GetResponseStream())
                responseParameters = oAuthParameter.Parse(sr.ReadToEnd())
                Dim reqToken As String = Nothing
                Dim reqTokenSecret As String = Nothing
                For Each param As oAuthParameter In responseParameters
                    If param.Name = "oauth_token" Then
                        reqToken = param.Value
                    End If
                    If param.Name = "oauth_token_secret" Then
                        reqTokenSecret = param.Value
                    End If
                Next
                If reqToken Is Nothing OrElse reqTokenSecret Is Nothing Then Throw New InvalidOperationException()
                Return New OAuthRequestToken(reqToken, reqTokenSecret)
            Finally
                If resp IsNot Nothing Then resp.Close()
            End Try
        End Function

        Public Function RequestAccessToken(ByVal verifier As [String], ByVal requestToken As OAuthRequestToken, ByVal accessTokenUrl As [String]) As oAuthAccessToken

            Dim oauth_consumer_key As [String] = _consumerKey
            Dim oauth_token As [String] = requestToken.TokenValue
            Dim oauth_signature_method As [String] = "HMAC-SHA1"
            Dim oauth_timestamp As [String] = ((DateTime.UtcNow.Ticks - New DateTime(1970, 1, 1).Ticks) \ (1000 * 10000)).ToString()
            Dim oauth_nonce As [String] = Guid.NewGuid().ToString()
            Dim oauth_callback As [String] = "oob"

            Dim oauth_signature As [String] = CreateHMACSHA1Signature(
                                        "POST",
                                        accessTokenUrl,
                                        oAuthParameter.ConCatAsArray(New oAuthParameter() {
                                                            New oAuthParameter("oauth_consumer_key", oauth_consumer_key),
                                                            New oAuthParameter("oauth_token", oauth_token),
                                                            New oAuthParameter("oauth_signature_method", oauth_signature_method),
                                                            New oAuthParameter("oauth_timestamp", oauth_timestamp),
                                                            New oAuthParameter("oauth_nonce", oauth_nonce),
                                                            New oAuthParameter("oauth_verifier", verifier)}),
                                        _consumerSecret,
                                        requestToken.TokenSecret)

            Dim data As String = "oauth_consumer_key=" & oAuthParameter.EncodeParameterString(oauth_consumer_key) & "&" &
                        "oauth_token=" & oAuthParameter.EncodeParameterString(oauth_token) & "&" &
                        "oauth_signature_method=" & oAuthParameter.EncodeParameterString(oauth_signature_method) & "&" &
                        "oauth_signature=" & oAuthParameter.EncodeParameterString(oauth_signature) & "&" &
                        "oauth_timestamp=" & oAuthParameter.EncodeParameterString(oauth_timestamp) & "&" &
                        "oauth_nonce=" & oAuthParameter.EncodeParameterString(oauth_nonce) & "&" &
                        "oauth_verifier=" & oAuthParameter.EncodeParameterString(verifier)

            Dim req As HttpWebRequest = System.Net.WebRequest.Create(accessTokenUrl)
            If Proxy IsNot Nothing Then req.Proxy = Proxy
            req.Method = "POST"
            req.ServicePoint.Expect100Continue = False
            req.UserAgent = GBAU_UserAgent
            req.ContentType = "application/x-www-form-urlencoded"
            req.Headers.Add("Accept-Encoding", "gzip")
            req.AutomaticDecompression = Net.DecompressionMethods.GZip
            Dim encoding As New ASCIIEncoding
            Dim TabBytes As Byte() = encoding.GetBytes(data)
            req.ContentLength = TabBytes.Length
            Dim Writer As Stream = req.GetRequestStream
            Writer.Write(TabBytes, 0, TabBytes.Length)
            Writer.Close()
            Dim resp As HttpWebResponse = Nothing
            Try
                resp = DirectCast(req.GetResponse(), HttpWebResponse)
                Dim sr As New StreamReader(resp.GetResponseStream())
                responseParameters = oAuthParameter.Parse(sr.ReadToEnd())
                Dim accessToken As [String] = Nothing
                Dim accessTokenSecret As [String] = Nothing
                For Each param As oAuthParameter In responseParameters
                    If param.Name = "oauth_token" Then
                        accessToken = param.Value
                    End If

                    If param.Name = "oauth_token_secret" Then
                        accessTokenSecret = param.Value
                    End If
                Next
                If accessToken Is Nothing OrElse accessTokenSecret Is Nothing Then Throw New InvalidOperationException()
                Return New oAuthAccessToken(accessToken, accessTokenSecret)
            Finally
                If resp IsNot Nothing Then resp.Close()
            End Try
        End Function

        Public Function AccessProtectedResource(ByVal accessToken As oAuthAccessToken, ByVal urlString As String, ByVal method As [String], ByVal authorizationRealm As [String], ByVal queryParameters As oAuthParameter()) As HttpWebResponse
            Dim uri As New Uri(urlString)
            ' If additionalParameters Is Nothing Then additionalParameters = New Parameter(-1) {}
            If Not (method.Equals("GET") OrElse method.Equals("POST") OrElse method.Equals("PUT") OrElse method.Equals("DELETE")) Then Throw New ArgumentException("La méthode doit être GET,PUT,DELETE ou POST")
            If uri.Query.Length > 0 Then Throw New ArgumentException("Parametres de la requete ne doivent pas être passé dans l'URL." & vbCr & vbLf & "Passer les parametres dans le parametre 'QueryParameters' de la methode.")
            If queryParameters Is Nothing Then queryParameters = New oAuthParameter(-1) {}

            Dim oauth_consumer_key As [String] = _consumerKey
            Dim oauth_token As [String] = accessToken.TokenValue
            Dim oauth_signature_method As [String] = "HMAC-SHA1"
            Dim oauth_timestamp As [String] = ((DateTime.UtcNow.Ticks - New DateTime(1970, 1, 1).Ticks) \ (1000 * 10000)).ToString()
            Dim oauth_nonce As [String] = Guid.NewGuid().ToString()

            Dim request As HttpWebRequest = System.Net.WebRequest.Create(urlString & (If(method.Equals("GET") AndAlso queryParameters.Length > 0, "?" & oAuthParameter.ConCat(queryParameters), "")))

            If Proxy IsNot Nothing Then request.Proxy = Proxy

            request.ServicePoint.Expect100Continue = False
            request.UserAgent = GBAU_UserAgent
            request.Headers.Add("Accept-Encoding", "gzip")
            request.AutomaticDecompression = Net.DecompressionMethods.GZip
            request.Method = method
            request.Timeout = 5000
            Dim oauth_signature As [String]
            If method.Equals("POST") And queryParameters.Count > 0 Then
                oauth_signature = CreateHMACSHA1Signature(request.Method,
                                                        urlString,
                                                        oAuthParameter.ConCatAsArray(New oAuthParameter() {
                                                                                    New oAuthParameter("oauth_consumer_key", oauth_consumer_key),
                                                                                    New oAuthParameter("oauth_token", oauth_token),
                                                                                    New oAuthParameter("oauth_signature_method", oauth_signature_method),
                                                                                    New oAuthParameter("oauth_timestamp", oauth_timestamp),
                                                                                    New oAuthParameter("oauth_nonce", oauth_nonce)}),
                                                        _consumerSecret,
                                                        accessToken.TokenSecret)
            Else
                oauth_signature = CreateHMACSHA1Signature(request.Method,
                                                        urlString,
                                                        oAuthParameter.ConCatAsArray(New oAuthParameter() {
                                                                                    New oAuthParameter("oauth_consumer_key", oauth_consumer_key),
                                                                                    New oAuthParameter("oauth_token", oauth_token),
                                                                                    New oAuthParameter("oauth_signature_method", oauth_signature_method),
                                                                                    New oAuthParameter("oauth_timestamp", oauth_timestamp),
                                                                                    New oAuthParameter("oauth_nonce", oauth_nonce)},
                                                                                queryParameters),
                                                        _consumerSecret,
                                                        accessToken.TokenSecret)
            End If



            request.Headers.Add("Authorization: OAuth " & "realm=""" & authorizationRealm & """," &
                                "oauth_consumer_key=""" & oAuthParameter.EncodeParameterString(oauth_consumer_key) & """," &
                                "oauth_token=""" & oAuthParameter.EncodeParameterString(oauth_token) & """," &
                                "oauth_signature_method=""" & oAuthParameter.EncodeParameterString(oauth_signature_method) & """," &
                                "oauth_signature=""" & oAuthParameter.EncodeParameterString(oauth_signature) & """," &
                                "oauth_timestamp=""" & oAuthParameter.EncodeParameterString(oauth_timestamp) & """," &
                                "oauth_nonce=""" & oAuthParameter.EncodeParameterString(oauth_nonce) & """")

            If method.Equals("POST") Then
                Dim contents As [String] = oAuthParameter.ConCatJson(queryParameters)
                Dim encoding As New ASCIIEncoding
                Dim TabBytes As Byte() = encoding.GetBytes(contents)
                request.ContentType = "application/json"
                request.ContentLength = TabBytes.Length
                Dim Writer As Stream = request.GetRequestStream
                Writer.Write(TabBytes, 0, TabBytes.Length)
                Writer.Close()
            End If
            Dim HttpDiscogsResponse As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)

            Return HttpDiscogsResponse ' TryCast(request.GetResponse(), HttpWebResponse)
        End Function

        Private Shared Function CreateHMACSHA1Signature(ByVal method As [String], ByVal url As [String], ByVal parameterArray As oAuthParameter(), ByVal consumerSecret As [String], ByVal tokenSecret As [String]) As [String]
            If consumerSecret Is Nothing Then Throw New NullReferenceException()
            If tokenSecret Is Nothing Then tokenSecret = ""
            method = method.ToUpper()
            url = url.ToLower()
            Dim uri As New Uri(url)
            url = uri.Scheme & "://" & uri.Host & (If((uri.Scheme.Equals("http") AndAlso uri.Port = 80 OrElse uri.Scheme.Equals("https") AndAlso uri.Port = 443), "", uri.Port.ToString())) & uri.AbsolutePath
            Dim concatenatedParameter As [String] = oAuthParameter.ConcatToNormalize(parameterArray)
            Dim alg As New HMACSHA1(encode(oAuthParameter.EncodeParameterString(consumerSecret) & "&" & oAuthParameter.EncodeParameterString(tokenSecret)))
            Return System.Convert.ToBase64String(alg.ComputeHash(encode(oAuthParameter.EncodeParameterString(method) & "&" & oAuthParameter.EncodeParameterString(url) & "&" & oAuthParameter.EncodeParameterString(concatenatedParameter))))
        End Function

        Public Shared Function BuildUserAuthorizationURL(ByVal userAuthorizationUrl As [String], ByVal requestToken As OAuthRequestToken) As [String]
            Dim uri As New Uri(userAuthorizationUrl)
            Return uri.OriginalString & (If(uri.Query IsNot Nothing AndAlso uri.Query.Length > 0, "&", "?")) & "oauth_token=" & oAuthParameter.EncodeParameterString(requestToken.TokenValue)
        End Function

        Private Shared Function encode(ByVal val As [String]) As Byte()
            Dim ms As New MemoryStream()
            Dim sw As New StreamWriter(ms, Encoding.ASCII)
            sw.Write(val)
            sw.Flush()
            Return ms.ToArray()
        End Function

        Private Shared Function decode(ByVal val As Byte()) As [String]
            Dim ms As New MemoryStream(val)
            Dim sr As New StreamReader(ms, Encoding.ASCII)
            Return sr.ReadToEnd()
        End Function

        Private Shared Function CreateRequestInDefault(ByVal uri As [String]) As HttpWebRequest
            Return DirectCast(WebRequest.Create(uri), HttpWebRequest)
        End Function

        '     Private Shared Function GetResponseInSynch(ByVal req As HttpWebRequest, ByVal syncResult As IAsyncResult) As HttpWebResponse
        '
        '        Dim timeOutCatchThread As Thread = Nothing
        '        Dim result As HttpWebResponse = Nothing
        '
        '        Dim timeOutCatchLockObject As New [Object]()
        '
        '            Try
        '
        '                If req.Timeout > 0 Then
        '					timeOutCatchThread = New Thread(Function() Do
        '        Dim waitStartClock As DateTime = DateTime.Now
        '
        '                    While DateTime.Now.Ticks < waitStartClock.Ticks + req.Timeout * 10000
        '                        SyncLock timeOutCatchLockObject
        '                            If result IsNot Nothing Then
        '                                Return
        '                            End If
        '                        End SyncLock
        '
        '
        '                Thread.Sleep(100)
        '            End While
        '
        '
        '            req.Abort()
        '			End Function)
        '
        '			timeOutCatchThread.Start()
        '		End If
        '
        'Dim response As HttpWebResponse = DirectCast(req.EndGetResponse(syncResult), HttpWebResponse)
        '
        '		SyncLock timeOutCatchLockObject
        '			result = response
        '		End SyncLock
        '
        '		timeOutCatchThread.Join(1000)
        '
        '
        '		Return result
        '	Finally
        '		If timeOutCatchThread IsNot Nothing AndAlso (timeOutCatchThread.ThreadState And ThreadState.Running) = ThreadState.Running Then
        '			timeOutCatchThread.Abort()
        '		End If
        '	End Try
        'End Function

        ''' <summary>
        ''' User can specify the factory method for creating request instance passing RequestFactoryMethod delegate.
        ''' Specifying the delegate, user can access much of the HttpWebRequest object.
        ''' RequestFactoryMethod delegate can be used in calling ObtainUnauthorizedRequestToken, RequestAccessToken, AccessProtectedResource, BeginAccessProtectedResource methods.
        ''' </summary>
        Public Delegate Function RequestFactoryMethod(ByVal uri As [String]) As HttpWebRequest

    End Class
End Namespace
