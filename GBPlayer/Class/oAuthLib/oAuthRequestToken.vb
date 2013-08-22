Imports System.Collections.Generic
Imports System.Text

Namespace OAuthLib
    Public Class OAuthRequestToken
        Private _tokenValue As String
        Private _tokenSecret As String
        Public ReadOnly Property TokenValue() As String
            Get
                Return _tokenValue
            End Get
        End Property
        Public ReadOnly Property TokenSecret() As String
            Get
                Return _tokenSecret
            End Get
        End Property
        Public Sub New(ByVal tokenValue As String, ByVal tokenSecret As String)
            _tokenValue = tokenValue
            _tokenSecret = tokenSecret
        End Sub

    End Class
End Namespace