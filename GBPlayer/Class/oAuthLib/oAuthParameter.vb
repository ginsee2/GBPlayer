Imports System.Collections.Generic
Imports System.Text
Imports System.Net
Imports System.Globalization

Namespace OAuthLib
    Public Class oAuthParameter
        Private _name As String
        Private _value As String

        Public ReadOnly Property Name() As String
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Value() As String
            Get
                Return _value
            End Get
        End Property
        Public Sub New(ByVal name As String, ByVal value As String)
            _name = name
            _value = value
        End Sub

        Private Shared Function SortToNormalize(ByVal parameterArray As oAuthParameter()) As oAuthParameter()
            Dim l As New List(Of oAuthParameter)(parameterArray)
            l.Sort(New ParameterComparer())
            Return l.ToArray()
        End Function

        Friend Shared Function ConcatToNormalize(ByVal parameterArray As oAuthParameter()) As String
            parameterArray = SortToNormalize(parameterArray)
            Return ConCat(parameterArray, "")
        End Function

        Friend Shared Function ConCat(ByVal parameterArray As oAuthParameter()) As String
            Return ConCat(parameterArray, "")
        End Function

        Friend Shared Function ConCatJson(ByVal parameterArray As oAuthParameter()) As String
            Dim ChaineRetour As String = "{"
            For Each i In parameterArray
                If ChaineRetour.Count > 1 Then ChaineRetour &= ", "
                If Left(i._value, 2) = "#N" Then
                    ChaineRetour &= """" & i._name & """" & ": " & ExtraitChaine(i._value, "#N", "", 2)
                ElseIf Left(i._value, 2) = "#F" Then
                    ChaineRetour &= """" & i._name & """" & ": " & ExtraitChaine(i._value, "#F", "", 2)
                Else
                    ChaineRetour &= """" & i._name & """" & ": " & """" & i._value & """"
                End If
            Next
            Return ChaineRetour & "}"
        End Function

        Friend Shared Function ConCat(ByVal parameterArray As oAuthParameter(), ByVal qutationMark As [String]) As String
            If parameterArray.Length = 0 Then
                Return ""
            End If

            Dim sb As New StringBuilder()
            For Each param As oAuthParameter In parameterArray
                sb.Append(qutationMark & EncodeParameterString(param._name) & qutationMark)
                sb.Append("=")
                sb.Append(qutationMark & EncodeParameterString(param._value) & qutationMark)
                sb.Append("&")
            Next

            sb.Remove(sb.Length - 1, 1)
            Return sb.ToString()
        End Function

        Friend Shared Function ConCatAsArray(ByVal ParamArray parameterArrayArray As oAuthParameter()()) As oAuthParameter()
            Dim result As New List(Of oAuthParameter)()
            For Each array As oAuthParameter() In parameterArrayArray
                result.AddRange(array)
            Next
            Return result.ToArray()
        End Function

        Friend Shared Function Parse(ByVal source As [String]) As oAuthParameter()
            If source = "" Then Return Nothing
            Dim list As New List(Of oAuthParameter)()
            Dim nameAndValSetArray As String() = source.Split("&"c)
            For Each nameAndValSet As String In nameAndValSetArray
                Dim nameAndVal As String() = nameAndValSet.Split("="c)
                nameAndVal(0) = Uri.UnescapeDataString(nameAndVal(0))
                nameAndVal(1) = Uri.UnescapeDataString(nameAndVal(1))
                list.Add(New oAuthParameter(nameAndVal(0), nameAndVal(1)))
            Next
            Return list.ToArray()
        End Function

        Friend Shared Function EncodeParameterString(ByVal val As [String]) As [String]
            Dim sb As New StringBuilder()
            For Each c As Char In val
                'ALPHA
                'ALPHA
                'DIGIT
                '"-"
                '"."
                '"_"
                If ("A"c <= c AndAlso c <= "Z"c) OrElse ("a"c <= c AndAlso c <= "z"c) OrElse ("0"c <= c AndAlso c <= "9"c) _
                    OrElse c = "-"c OrElse c = "."c OrElse c = "_"c OrElse c = "~"c Then
                    '"~"
                    sb.Append(c)
                Else
                    Dim encoded As Byte() = Encoding.UTF8.GetBytes(New Char() {c})
                    For i As Integer = 0 To encoded.Length - 1
                        sb.Append("%"c)
                        sb.Append(encoded(i).ToString("X2"))
                    Next
                End If
            Next
            Return sb.ToString()
        End Function
    End Class

    Public Class ParameterComparer
        Implements IComparer(Of oAuthParameter)

        Public Function Compare(ByVal x As oAuthParameter, ByVal y As oAuthParameter) As Integer Implements IComparer(Of oAuthParameter).Compare
            If Not x.Name.Equals(y.Name) Then
                Return oAuthParameter.EncodeParameterString(x.Name).CompareTo(oAuthParameter.EncodeParameterString(y.Name))
            Else
                Return oAuthParameter.EncodeParameterString(x.Value).CompareTo(oAuthParameter.EncodeParameterString(y.Value))
            End If
        End Function
    End Class

End Namespace