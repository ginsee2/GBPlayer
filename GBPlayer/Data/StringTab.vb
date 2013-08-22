Public Class StringTab
    Public Property Liste As Dictionary(Of String, String)
    Public Sub New()
    End Sub
    Public Sub New(Chaine As String)
        Dim ComposChaine As String() = Split(Chaine, "<,>")
        If Liste Is Nothing Then Liste = New Dictionary(Of String, String)
        For Each i In ComposChaine
            Dim Key As String = ExtraitChaine(i, "", "<:>")
            Dim Value As String = ExtraitChaine(i, "<:>", "", 3)
            If Not Liste.ContainsKey(Key) Then
                Liste.Add(Key, Value)
            End If
        Next
    End Sub
    Public Function GetValue(Key As String) As String
        If Liste.ContainsKey(Key) Then
            Return Liste.Item(Key)
        End If
    End Function

    Public Function IsEmpty() As Boolean
        If Liste Is Nothing Then Return True
        If Liste.Count = 0 Then Return True
        Return False
    End Function
    Public Sub Update(Key As String, value As String)
        If Liste Is Nothing Then Liste = New Dictionary(Of String, String)
        If Not Liste.ContainsKey(Key) Then
            Liste.Add(Key, value)
        End If
    End Sub
    Public Overrides Function ToString() As String
        Dim Chaine As String = ""
        For Each i In Liste
            If Chaine <> "" Then Chaine &= "<,>"
            Chaine &= i.Key & "<:>" & i.Value
        Next
        Return Chaine
    End Function
End Class
