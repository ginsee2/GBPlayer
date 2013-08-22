'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 01/08/10
'DESCRIPTION :Classe de stockage des informations de configuration personnelle
Imports System.IO
Imports System.Reflection
Imports System.Xml.Serialization

'***********************************************************************************************
'---------------------------------PROPRIETES PUBLIQUES DE LA CLASSE-----------------------------
'***********************************************************************************************
Public Class ConfigPerso
    Private Const GBAU_NOMDOSSIER_STOCKAGECONFIG = "GBDev\GBPlayer\Config"
    Private Const GBAU_NOMFICHIER_STOCKAGECONFIG = "Config 3.0.17.xml"
    Private Const GBAU_NOMRESSOURCE = "gbDev.ConfigPerso.xml"
    Private Const GBAU_VERSIONCONFIG = "3.0.16"
    Public Nom As String = "GBPLayer"
    Public Version As String = GBAU_VERSIONCONFIG
    Public MAINWINDOW_Position As Point = New Point(0, 0)
    Public MAINWINDOW_Size As Size = New Size(200, 200)
    Public PLAYERVOLUME0 As String = "100"
    Public LISTEREPERTOIRES_Racine As String = ""
    Public LISTEREPERTOIRES_RepertoireEnCours As String = ""
    Public LISTEFICHIERSMP3_ListeColonnes As New List(Of String)  'Liste des colonnes de la liste fichier avec lors configuration
    Public LISTEFICHIERSMP3_ColonneTriee As String = "Nom;A"
    Public LISTECOLLECTION_ListeColonnes As New List(Of String)  'Liste des colonnes de la liste vinyl avec lors configuration
    Public LISTECOLLECTION_ColonneTriee As String = "Nom;A"
    Public LISTECOLLECTION_BackUpDirectory As String = ""
    Public WANTLIST_ListeColonnes As New List(Of String)  'Liste des colonnes de la liste des recherches avec lors configuration
    Public WANTLIST_ColonneTriee As String = "Nom;A"
    Public LISTECD_ListeColonnes As New List(Of String)  'Liste des colonnes de la liste vinyl avec lors configuration
    Public LISTECD_ColonneTriee As String = "Nom;A"
    Public FILESINFOS_ParametrageMenuTitre As New List(Of String) 'Paramétrage du menu contextuel sur la zone titre du tag ID3
    Public FILESINFOS_ChaineFormattageNom As String = "%ARTISTE% - %TITRE%"
    Public FILESINFOS_ChaineExtractionInfos As String = "%ARTISTE%-%TITRE%"
    Public DISCOGSCONNECTION_consumerKey As String = ""
    Public DISCOGSCONNECTION_consumerSecret As String = ""
    Public DISCOGSCONNECTION_tokenValue As String = ""
    Public DISCOGSCONNECTION_tokenSecret As String = ""
    Public USER_name As String = ""
    Public USER_ID As String = ""
    Public SELLLIST_ListeColonnes As New List(Of String)  'Liste des colonnes de la liste des vinyls a vendre avec lors configuration
    Public SELLLIST_ColonneTriee As String = "Nom;A"
    Public PHONESYNCHRO_Directory As String = "C:\"
    Public PHONESYNCHRO_ListeColonnes As New List(Of String)
    Public PHONESYNCHRO_ColonneTriee As String = "Nom;A"

    Private Shared PathFichierConfig As String
    Private Shared OpenConfigPerso As ConfigPerso

    '***********************************************************************************************
    '---------------------------------PROCEDURES SHARED DE LA CLASSE--------------------------------
    '***********************************************************************************************
    Public Shared Function GetConfigPersoPath() As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        PathFichierConfig = RepDest & "\" & GBAU_NOMFICHIER_STOCKAGECONFIG
        OpenConfigPerso = Nothing
        Return PathFichierConfig
    End Function
    Public Shared Function LoadConfig() As ConfigPerso
        Dim SauvegardeExVersion As ConfigPerso = Nothing
        If OpenConfigPerso IsNot Nothing Then
            Return OpenConfigPerso
        Else
            Dim Donnees As ConfigPerso
            Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
            If Not Directory.Exists(RepDest) Then
                Directory.CreateDirectory(RepDest)
            End If
            PathFichierConfig = RepDest & "\" & GBAU_NOMFICHIER_STOCKAGECONFIG
            If Not File.Exists(PathFichierConfig) Then
                SauvegardeExVersion = SauvegardeVersionPrecedente()
                Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GBAU_NOMRESSOURCE)
                Dim b As FileStream = New FileStream(PathFichierConfig, FileMode.Create)
                s.CopyTo(b)
                b.Close()
            End If
            Try
                Using s As FileStream = New FileStream(PathFichierConfig, FileMode.Open, FileAccess.ReadWrite)
                    Dim xs As New XmlSerializer(GetType(ConfigPerso))
                    Donnees = CType(xs.Deserialize(s), ConfigPerso)
                    If SauvegardeExVersion IsNot Nothing Then
                        RestaureExVersion(Donnees, SauvegardeExVersion)
                        s.Seek(0, SeekOrigin.Begin)
                        xs.Serialize(s, Donnees)
                    End If
                End Using
                OpenConfigPerso = Donnees
                Return OpenConfigPerso
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Erreur configuration", ex.Message, Nothing)
                Return Nothing
            End Try
        End If
    End Function
    Public Shared Sub SaveConfig(ByVal Donnees As ConfigPerso)
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        Donnees.Version = GBAU_VERSIONCONFIG
        Dim xs As New XmlSerializer(GetType(ConfigPerso))
        Using configFile As FileStream = File.Open(PathFichierConfig, FileMode.Create, FileAccess.Write)
            xs.Serialize(configFile, Donnees)
            OpenConfigPerso = Nothing
        End Using
    End Sub
    Public Shared Sub UpdateListeColonnes(ByRef Donnees As List(Of String), ByVal Liste As GridViewColumnCollection)
        Donnees.Clear()
        For Each i As GridViewColumn In Liste
            Donnees.Add(CType(i.Header, GridViewColumnHeader).Content.ToString & _
                                                        ";" & Liste.IndexOf(i).ToString & _
                                                        "/" & i.Width.ToString)
        Next
    End Sub
    Public Shared Sub UpdateListeColonnesTag(ByRef Donnees As List(Of String), ByVal Liste As GridViewColumnCollection)
        Donnees.Clear()
        For Each i As GridViewColumn In Liste
            Donnees.Add(CType(i.Header, GridViewColumnHeader).Tag.ToString & _
                                                         ";" & Liste.IndexOf(i).ToString & _
                                                         "/" & i.Width.ToString)
            ' Donnees.Add(CType(i.Header, GridViewColumnHeader).Tag.ToString & _
            '                                               ";" & Liste.IndexOf(i).ToString & _
         '                                               "/" & i.Width.ToString)
        Next
    End Sub
    Private Shared Function SauvegardeVersionPrecedente() As ConfigPerso
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_STOCKAGECONFIG
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        Dim TabVersion = Split(GBAU_VERSIONCONFIG, ".")
        Dim CheminVersionPrecedente As String
        Dim ExVersion = CInt(TabVersion(2)) - 1
        CheminVersionPrecedente = RepDest & "\Config " & TabVersion(0) & "." & TabVersion(1) & "." & ExVersion & ".xml"
        While Not File.Exists(CheminVersionPrecedente)
            ExVersion -= 1
            CheminVersionPrecedente = RepDest & "\Config " & TabVersion(0) & "." & TabVersion(1) & "." & ExVersion & ".xml"
            If ExVersion = 0 Then Return Nothing
        End While
        Dim RetourExConfig As ConfigPerso = New ConfigPerso
        Dim DocXCollection As XDocument = XDocument.Load(CheminVersionPrecedente)
        Debug.Print(CSng(RemplaceChaine(ExtraitChaine(DocXCollection.Root.<Version>.Value, ".", ""), ".", ",")))
        Dim VersionPrecedente = CSng(RemplaceChaine(ExtraitChaine(DocXCollection.Root.<Version>.Value, ".", ""), ".", ","))
        If VersionPrecedente >= 0.14 Then
            RetourExConfig.Version = DocXCollection.Root.<Version>.Value
            RetourExConfig.MAINWINDOW_Position = New Point(DocXCollection.Root.<MAINWINDOW_Position>.<X>.Value, DocXCollection.Root.<MAINWINDOW_Position>.<Y>.Value)
            RetourExConfig.MAINWINDOW_Size = New Size(DocXCollection.Root.<MAINWINDOW_Size>.<Width>.Value, DocXCollection.Root.<MAINWINDOW_Size>.<Height>.Value)
            RetourExConfig.PLAYERVOLUME0 = DocXCollection.Root.<PLAYERVOLUME0>.Value
            RetourExConfig.LISTEREPERTOIRES_Racine = DocXCollection.Root.<LISTEREPERTOIRES_Racine>.Value
            RetourExConfig.LISTEREPERTOIRES_RepertoireEnCours = DocXCollection.Root.<LISTEREPERTOIRES_RepertoireEnCours>.Value
            RetourExConfig.LISTEFICHIERSMP3_ListeColonnes = New List(Of String)
            For Each v In From i In DocXCollection.Root.<LISTEFICHIERSMP3_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.LISTEFICHIERSMP3_ListeColonnes.Add(v)
            Next v
            RetourExConfig.LISTEFICHIERSMP3_ColonneTriee = DocXCollection.Root.<LISTEFICHIERSMP3_ColonneTriee>.Value
            RetourExConfig.LISTECOLLECTION_ListeColonnes = New List(Of String)
            For Each v In From i In DocXCollection.Root.<LISTECOLLECTION_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.LISTECOLLECTION_ListeColonnes.Add(v)
            Next v
            RetourExConfig.LISTECOLLECTION_ColonneTriee = DocXCollection.Root.<LISTECOLLECTION_ColonneTriee>.Value
            RetourExConfig.LISTECOLLECTION_BackUpDirectory = DocXCollection.Root.<LISTECOLLECTION_BackUpDirectory>.Value
            RetourExConfig.WANTLIST_ListeColonnes = New List(Of String)
            For Each v In From i In DocXCollection.Root.<WANTLIST_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.WANTLIST_ListeColonnes.Add(v)
            Next v
            RetourExConfig.WANTLIST_ColonneTriee = DocXCollection.Root.<WANTLIST_ColonneTriee>.Value
            RetourExConfig.LISTECD_ListeColonnes = New List(Of String)
            For Each v In From i In DocXCollection.Root.<LISTECD_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.LISTECD_ListeColonnes.Add(v)
            Next v
            RetourExConfig.LISTECD_ColonneTriee = DocXCollection.Root.<LISTECD_ColonneTriee>.Value
            RetourExConfig.FILESINFOS_ParametrageMenuTitre = New List(Of String)
            For Each v In From i In DocXCollection.Root.<FILESINFOS_ParametrageMenuTitre>.<string>
                                                            Select i.Value
                RetourExConfig.FILESINFOS_ParametrageMenuTitre.Add(v)
            Next v
            RetourExConfig.FILESINFOS_ChaineFormattageNom = DocXCollection.Root.<FILESINFOS_ChaineFormattageNom>.Value
            RetourExConfig.FILESINFOS_ChaineExtractionInfos = DocXCollection.Root.<FILESINFOS_ChaineExtractionInfos>.Value
            RetourExConfig.DISCOGSCONNECTION_consumerKey = DocXCollection.Root.<DISCOGSCONNECTION_consumerKey>.Value
            RetourExConfig.DISCOGSCONNECTION_consumerSecret = DocXCollection.Root.<DISCOGSCONNECTION_consumerSecret>.Value
            RetourExConfig.DISCOGSCONNECTION_tokenValue = DocXCollection.Root.<DISCOGSCONNECTION_tokenValue>.Value
            RetourExConfig.DISCOGSCONNECTION_tokenSecret = DocXCollection.Root.<DISCOGSCONNECTION_tokenSecret>.Value
        End If
        If VersionPrecedente >= 0.15 Then
            RetourExConfig.USER_name = DocXCollection.Root.<USER_name>.Value
            RetourExConfig.USER_ID = DocXCollection.Root.<USER_ID>.Value
        End If
        If VersionPrecedente >= 0.16 Then
            For Each v In From i In DocXCollection.Root.<SELLLIST_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.SELLLIST_ListeColonnes.Add(v)
            Next v
            RetourExConfig.SELLLIST_ColonneTriee = DocXCollection.Root.<SELLLIST_ColonneTriee>.Value
        End If
        If VersionPrecedente >= 0.17 Then
            For Each v In From i In DocXCollection.Root.<PHONESYNCHRO_ListeColonnes>.<string>
                                                            Select i.Value
                RetourExConfig.PHONESYNCHRO_ListeColonnes.Add(v)
            Next v
            RetourExConfig.PHONESYNCHRO_ColonneTriee = DocXCollection.Root.<PHONESYNCHRO_ColonneTriee>.Value
            RetourExConfig.PHONESYNCHRO_Directory = DocXCollection.Root.<PHONESYNCHRO_Directory>.Value
        End If
        Return RetourExConfig
    End Function
    Private Shared Sub RestaureExVersion(ByRef NouvelleConfig As ConfigPerso, ByVal ExConfig As ConfigPerso)
        Dim NouvelleVersion = CSng(RemplaceChaine(ExtraitChaine(ExConfig.Version, ".", ""), ".", ","))
        If NouvelleVersion >= 0.14 Then
            NouvelleConfig.MAINWINDOW_Position = ExConfig.MAINWINDOW_Position
            NouvelleConfig.MAINWINDOW_Size = ExConfig.MAINWINDOW_Size
            NouvelleConfig.PLAYERVOLUME0 = ExConfig.PLAYERVOLUME0
            NouvelleConfig.LISTEREPERTOIRES_Racine = ExConfig.LISTEREPERTOIRES_Racine
            NouvelleConfig.LISTEREPERTOIRES_RepertoireEnCours = ExConfig.LISTEREPERTOIRES_RepertoireEnCours
            NouvelleConfig.LISTEFICHIERSMP3_ListeColonnes = ExConfig.LISTEFICHIERSMP3_ListeColonnes
            NouvelleConfig.LISTEFICHIERSMP3_ColonneTriee = ExConfig.LISTEFICHIERSMP3_ColonneTriee
            NouvelleConfig.LISTECOLLECTION_ListeColonnes = ExConfig.LISTECOLLECTION_ListeColonnes
            NouvelleConfig.LISTECOLLECTION_ColonneTriee = ExConfig.LISTECOLLECTION_ColonneTriee
            NouvelleConfig.LISTECOLLECTION_BackUpDirectory = ExConfig.LISTECOLLECTION_BackUpDirectory
            NouvelleConfig.WANTLIST_ListeColonnes = ExConfig.WANTLIST_ListeColonnes
            NouvelleConfig.WANTLIST_ColonneTriee = ExConfig.WANTLIST_ColonneTriee
            NouvelleConfig.LISTECD_ListeColonnes = ExConfig.LISTECD_ListeColonnes
            NouvelleConfig.LISTECD_ColonneTriee = ExConfig.LISTECD_ColonneTriee
            NouvelleConfig.FILESINFOS_ParametrageMenuTitre = ExConfig.FILESINFOS_ParametrageMenuTitre
            NouvelleConfig.FILESINFOS_ChaineFormattageNom = ExConfig.FILESINFOS_ChaineFormattageNom
            NouvelleConfig.FILESINFOS_ChaineExtractionInfos = ExConfig.FILESINFOS_ChaineExtractionInfos
            NouvelleConfig.LISTEREPERTOIRES_RepertoireEnCours = ExConfig.LISTEREPERTOIRES_RepertoireEnCours
            NouvelleConfig.DISCOGSCONNECTION_consumerKey = ExConfig.DISCOGSCONNECTION_consumerKey
            NouvelleConfig.DISCOGSCONNECTION_consumerSecret = ExConfig.DISCOGSCONNECTION_consumerSecret
            NouvelleConfig.DISCOGSCONNECTION_tokenValue = ExConfig.DISCOGSCONNECTION_tokenValue
            NouvelleConfig.DISCOGSCONNECTION_tokenSecret = ExConfig.DISCOGSCONNECTION_tokenSecret
        End If
        If NouvelleVersion >= 0.15 Then
            NouvelleConfig.USER_name = ExConfig.USER_name
            NouvelleConfig.USER_ID = ExConfig.USER_ID
        End If
        If NouvelleVersion >= 0.16 Then
            NouvelleConfig.SELLLIST_ListeColonnes = ExConfig.SELLLIST_ListeColonnes
            NouvelleConfig.SELLLIST_ColonneTriee = ExConfig.SELLLIST_ColonneTriee
        End If
        If NouvelleVersion >= 0.17 Then
            NouvelleConfig.PHONESYNCHRO_ListeColonnes = ExConfig.PHONESYNCHRO_ListeColonnes
            NouvelleConfig.PHONESYNCHRO_ColonneTriee = ExConfig.PHONESYNCHRO_ColonneTriee
            NouvelleConfig.PHONESYNCHRO_Directory = ExConfig.PHONESYNCHRO_Directory
        End If
    End Sub
End Class

