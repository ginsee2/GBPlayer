'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 01/08/10
'DESCRIPTION :Classe de stockage des informations de configuration personnelle
Imports System.IO
Imports System.Reflection
Imports System.Xml.Serialization
Imports Serializer
Imports System.ComponentModel

'***********************************************************************************************
'---------------------------------PROPRIETES PUBLIQUES DE LA CLASSE-----------------------------
'***********************************************************************************************
Public Class ConfigApp
    Public Class ConfigPoint
        <ToSerialize> Public X As Double
        <ToSerialize> Public Y As Double
        Public Sub New()
        End Sub
        Public Sub New(X1 As Double, Y1 As Double)
            X = X1
            Y = Y1
        End Sub
    End Class
    Public Class ConfigSize
        <ToSerialize> Public Height As Double
        <ToSerialize> Public Width As Double
        Public Sub New()
        End Sub
        Public Sub New(Height1 As Double, Width1 As Double)
            Height = Height1
            Width = Width1
        End Sub
    End Class
    Public Class ColumnDescription
        <ToSerialize> Public Name As String
        <ToSerialize> Public Size As String
        Public Sub New()
        End Sub
        Public Sub New(AName As String, ASize As Double)
            Name = AName
            Size = ASize
        End Sub
    End Class
    Public Class DescriptionTri
        <ToSerialize> Public Name As String
        <ToSerialize> Public SortDirection As ListSortDirection
        Public Sub New()
        End Sub
        Public Sub New(AName As String, ASortDirection As ListSortDirection)
            Name = AName
            SortDirection = ASortDirection
        End Sub
    End Class
    Shared GBAU_NOMDOSSIER_STOCKAGECONFIG = "GBDev\GBPlayer\Config"
    Shared GBAU_NOMFICHIER_STOCKAGECONFIG = "Config"
    Shared GBAU_VERSIONCONFIG = "3.1.0"
    <ToSerialize> Public application_version As String = GBAU_VERSIONCONFIG
    <ToSerialize> Public mainWindow_position As ConfigPoint = New ConfigPoint(0, 0)
    <ToSerialize> Public mainWindow_size As ConfigSize = New ConfigSize(200, 200)
    <ToSerialize> Public player_volumes As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
    <ToSerialize> Public directoriesList_root As String = "C:\"
    <ToSerialize> Public directoriesList_activeDirectory As String = ""
    <ToSerialize> Public filesList_columns As New List(Of ColumnDescription)  'Liste des colonnes de la liste fichier avec lors configuration
    <ToSerialize> Public filesList_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public collectionList_columns As New List(Of ColumnDescription)  'Liste des colonnes de la liste vinyl avec lors configuration
    <ToSerialize> Public collectionList_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public collectionList_backupDirectory As String = ""
    <ToSerialize> Public wantList_columns As New List(Of ColumnDescription)  'Liste des colonnes de la liste des recherches avec lors configuration
    <ToSerialize> Public wantList_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public cdList_columns As New List(Of ColumnDescription)  'Liste des colonnes de la liste vinyl avec lors configuration
    <ToSerialize> Public cdList_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public phoneSynchro_directory As String = "C:\"
    <ToSerialize> Public phoneSynchro_columns As New List(Of ColumnDescription)
    <ToSerialize> Public phoneSynchro_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public sellList_columns As New List(Of ColumnDescription)  'Liste des colonnes de la liste des vinyls a vendre avec lors configuration
    <ToSerialize> Public sellList_sortColumn As DescriptionTri = New DescriptionTri("Nom", ListSortDirection.Ascending)
    <ToSerialize> Public filesInfos_settingTitleMenu As New List(Of String) 'Paramétrage du menu contextuel sur la zone titre du tag ID3
    <ToSerialize> Public filesInfos_stringFormat_fileName As String = "%ARTISTE% - %TITRE%"
    <ToSerialize> Public filesInfos_stringFormat_extractInfos As String = "%ARTISTE%-%TITRE%"
    <ToSerialize> Public discogsConnection_consumerKey As String = "SmqUcBibwxsZypHNoFZA"
    <ToSerialize> Public discogsConnection_consumerSecret As String = "igmnGepWhdpfgnlLycVkZegAtZsmtEIK"
    <ToSerialize> Public discogsConnection_tokenValue As String = ""
    <ToSerialize> Public discogsConnection_tokenSecret As String = ""
    <ToSerialize> Public user_name As String = "newUser"
    <ToSerialize> Public user_id As String = ""

    Public Sub New()
        player_volumes.Add("PLAYER0", 100)
    End Sub
    '***********************************************************************************************
    '---------------------------------PROCEDURES SHARED DE LA CLASSE--------------------------------
    '***********************************************************************************************
    Public Shared Function GetConfigPersoPath() As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        Dim PathFichierConfig As String = RepDest & "\" & GBAU_NOMFICHIER_STOCKAGECONFIG & " " & GBAU_VERSIONCONFIG & ".xml"
        Return PathFichierConfig
    End Function
    Public Shared Function LoadConfig() As ConfigApp
        Dim Donnees As ConfigApp = New ConfigApp
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        Dim PathFichierConfig As String = RepDest & "\" & GBAU_NOMFICHIER_STOCKAGECONFIG & " " & GBAU_VERSIONCONFIG & ".xml"
        Try
            Using s As FileStream = New FileStream(PathFichierConfig, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                Dim LecteurXML As XMLReader = New XMLReader(New StreamReader(s))
                Dim Serialiser As SerializeManager = New SerializeManager
                Serialiser.Unserialize(Donnees, "GBPLAYER", LecteurXML)
            End Using
        Catch ex As Exception
        End Try
        Return Donnees
    End Function
    Public Shared Sub SaveConfig(ByRef Donnees As ConfigApp)
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        Dim PathFichierConfig As String = RepDest & "\" & GBAU_NOMFICHIER_STOCKAGECONFIG & " " & GBAU_VERSIONCONFIG & ".xml"
        Donnees.application_version = GBAU_VERSIONCONFIG
        Try
            Using s As FileStream = New FileStream(PathFichierConfig, FileMode.Create, FileAccess.Write)
                Dim WriterXML As XMLWriter = New XMLWriter(New StreamWriter(s))
                Dim Serialiser As SerializeManager = New SerializeManager
                Serialiser.Serialize(Donnees, "GBPLAYER", WriterXML)
            End Using
        Catch ex As Exception
        End Try
    End Sub
    Public Shared Sub UpdateListeColonnes(ByRef Donnees As List(Of ColumnDescription), ByVal Liste As GridViewColumnCollection)
        Donnees.Clear()
        For Each i As GridViewColumn In Liste
            Donnees.Add(New ColumnDescription(CType(i.Header, GridViewColumnHeader).Content.ToString, i.Width))
        Next
    End Sub
    Public Shared Sub UpdateListeColonnesTag(ByRef Donnees As List(Of ColumnDescription), ByVal Liste As GridViewColumnCollection)
        Donnees.Clear()
        For Each i As GridViewColumn In Liste
            Donnees.Add(New ColumnDescription(CType(i.Header, GridViewColumnHeader).Tag.ToString, i.Width))
        Next
    End Sub
End Class