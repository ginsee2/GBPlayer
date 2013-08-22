'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 01/08/10
'DESCRIPTION :Classe de stockage des informations de configuration
Imports System.IO
Imports System.Reflection
Imports System.Xml.Serialization
'***********************************************************************************************
'---------------------------------PROPRIETES PUBLIQUES DE LA CLASSE-----------------------------
'***********************************************************************************************
Public Class TagID3Data
    Public Nom As String = "tagID3Object"
    Public Version As String = "10.0"
    Public TAGUTILISE_ID3v2 As New List(Of String)          'Liste des TAG utilisés en v2 avec leurs description
    Public CHAINEEXTRATION_ID3v2 As New List(Of String)     'Liste des mots clé pour extraction
    Public CONVERSION_ID3v22_ID3v23 As New List(Of String)  'Liste des conversions V22 vers V23
    Public CONVERSION_ID3v23_ID3v22 As New List(Of String)  'Liste des conversions V23 vers V22
    Private Const GBAU_NOMRESSOURCE = "gbDev.TagID3Data.xml"

    '***********************************************************************************************
    '---------------------------------PROCEDURES SHARED DE LA CLASSE--------------------------------
    '***********************************************************************************************
    Public Shared Function LoadConfig() As TagID3Data
        Try
            Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GBAU_NOMRESSOURCE)
            Dim xs As New XmlSerializer(GetType(TagID3Data))
            Dim Donnees As TagID3Data = CType(xs.Deserialize(s), TagID3Data)
            Return Donnees
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
        Return Nothing
    End Function
End Class
