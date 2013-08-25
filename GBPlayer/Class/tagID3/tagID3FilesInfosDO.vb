Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 30/12/10
'DESCRIPTION :Classes utilités pour la gestion liste de fichiers
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.ComponentModel
Imports System.IO
Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Runtime.Serialization
Imports System.Reflection
Imports System.Xml

'***********************************************************************************************
'-------------------------------CLASSE DE STOCKAGE INFOS FICHIERS DEPENDENCY--------------------
'***********************************************************************************************
<Serializable()>
Public Class tagID3FilesInfosDO
    Inherits DependencyObject
    Implements IDataErrorInfo
    Implements ISerializable

    Private Const GBAU_NOMDOSSIER_LISTESPERSO = "GBDev\GBPlayer\Data"
    Private Const GBAU_NOMFICHIER_LISTESPERSO = "PersonalList.xml"
    Private Const GBAU_NOMRESSOURCE = "gbDev.DataModelPersonalList.xml"
    Private Const GBAU_VERSIONLISTESPERSO = "1.0.2"
    Private Shared PathFichierListesPerso As String = ""

    Public Shared Function GetDataProvider() As Uri
        If PathFichierListesPerso = "" Then
            Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_LISTESPERSO
            If Not Directory.Exists(RepDest) Then
                Directory.CreateDirectory(RepDest)
            End If
            Try
                Dim DocXCollection As XDocument = Nothing
                PathFichierListesPerso = RepDest & "\" & GBAU_NOMFICHIER_LISTESPERSO
                If Not File.Exists(PathFichierListesPerso) Then
                    Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GBAU_NOMRESSOURCE)
                    Dim b As FileStream = New FileStream(PathFichierListesPerso, FileMode.Create)
                    s.CopyTo(b)
                    b.Close()
                End If
                'PARTIE MISE A JOUR DE LA STRUCTURE DU FICHIER
                DocXCollection = XDocument.Load(PathFichierListesPerso)
                If DocXCollection.Root.@Version <> GBAU_VERSIONLISTESPERSO Then
                    DocXCollection = UpdateFileXml(DocXCollection)
                    DocXCollection.Save(PathFichierListesPerso)
                End If
            Catch ex As Exception
                PathFichierListesPerso = ""
                Debug.Print("Erreur : " & ex.Message)
            End Try
        End If
        If PathFichierListesPerso <> "" Then Return New Uri(PathFichierListesPerso) Else Return Nothing
    End Function
    Private Shared Function UpdateFileXml(ByVal FichierExistant As XDocument) As XDocument
        Dim ListeVinylsxmlTemp As XElement
        If FichierExistant IsNot Nothing Then
            ListeVinylsxmlTemp = _
                        <LISTESTYLES>
                            <%= From i In FichierExistant.Root.<LISTESTYLES>.Elements _
                                 Select DuplicateElementStyle(i) %>
                        </LISTESTYLES>
            FichierExistant.Root.<LISTESTYLES>.Remove()
            FichierExistant.Root.Add(ListeVinylsxmlTemp)
            FichierExistant.Root.@Version = GBAU_VERSIONLISTESPERSO
        End If
        Return FichierExistant
    End Function
    Private Shared Function DuplicateElementStyle(ByVal Element As XElement) As XElement
        Return <Style><%= Element.Value %></Style>
    End Function
    Public Shared Function AddListElement(ByVal Document As XmlDocument, ByVal NomNouveauStyle As String, ByVal Donnee As XmlElement) As String
        Dim doc As New XmlDocument()
        doc.LoadXml("<Style>" & NomNouveauStyle & "</Style>")
        Dim NouveauStyle As XmlElement = doc.DocumentElement
        Dim newBook As XmlNode = Document.ImportNode(NouveauStyle, True)
        Document.SelectSingleNode("LISTESPERSO/LISTESTYLES").InsertAfter(newBook, Donnee)
        Document.Save(PathFichierListesPerso)
        Return NomNouveauStyle
    End Function
    Public Shared Sub DelListElement(ByVal Document As XmlDocument, ByVal Donnee As XmlElement)
        Dim NodeASelectionner As XmlElement = Donnee.NextSibling
        Document.SelectSingleNode("LISTESPERSO/LISTESTYLES").RemoveChild(Donnee)
        Document.Save(PathFichierListesPerso)
    End Sub

    'Public Property Id3v2Tag As boolean
    Public Shared ReadOnly Id3v2TagProperty As DependencyProperty = DependencyProperty.Register("Id3v2Tag",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Id3v2Tag() As Boolean
        Get
            Return DirectCast(GetValue(Id3v2TagProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(Id3v2TagProperty, value)
        End Set
    End Property

    'Public Property Id3v1Tag As boolean
    Public Shared ReadOnly Id3v1TagProperty As DependencyProperty = DependencyProperty.Register("Id3v1Tag",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Id3v1Tag() As Boolean
        Get
            Return DirectCast(GetValue(Id3v1TagProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(Id3v1TagProperty, value)
        End Set
    End Property

    'Public Property TagNormalise As boolean
    Public Shared ReadOnly TagNormaliseProperty As DependencyProperty = DependencyProperty.Register("TagNormalise",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property TagNormalise() As Boolean
        Get
            Return DirectCast(GetValue(TagNormaliseProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(TagNormaliseProperty, value)
        End Set
    End Property

    'Public Property FileReadOnly As boolean
    Public Shared ReadOnly FileReadOnlyProperty As DependencyProperty = DependencyProperty.Register("FileReadOnly",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property FileReadOnly() As Boolean
        Get
            Return DirectCast(GetValue(FileReadOnlyProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(FileReadOnlyProperty, value)
        End Set
    End Property

    'Public Property Nom As String
    Public Shared ReadOnly NomProperty As DependencyProperty = DependencyProperty.Register("Nom",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Nom() As String
        Get
            Return DirectCast(GetValue(NomProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(NomProperty, value)
        End Set
    End Property

    ' Public Property Taille As String
    Public Shared ReadOnly TailleProperty As DependencyProperty = DependencyProperty.Register("Taille",
        GetType(Integer), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property Taille() As Integer
        Get
            Return DirectCast(GetValue(TailleProperty), Integer)
        End Get
        Set(ByVal value As Integer)
            SetValue(TailleProperty, value)
        End Set
    End Property

    'Public Property Repertoire As String
    Public Shared ReadOnly RepertoireProperty As DependencyProperty = DependencyProperty.Register("Repertoire",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property Repertoire() As String
        Get
            Return DirectCast(GetValue(RepertoireProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(RepertoireProperty, value)
        End Set
    End Property

    'Public Property Artiste As String
    Public Shared ReadOnly ArtisteProperty As DependencyProperty = DependencyProperty.Register("Artiste",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Artiste() As String
        Get
            Return DirectCast(GetValue(ArtisteProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(ArtisteProperty, value)
        End Set
    End Property

    'Public Property Titre As String
    Public Shared ReadOnly TitreProperty As DependencyProperty = DependencyProperty.Register("Titre",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Titre() As String
        Get
            Return DirectCast(GetValue(TitreProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(TitreProperty, value)
        End Set
    End Property

    'Public Property SousTitre As String
    Public Shared ReadOnly SousTitreProperty As DependencyProperty = DependencyProperty.Register("SousTitre",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property SousTitre() As String
        Get
            Return DirectCast(GetValue(SousTitreProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(SousTitreProperty, value)
        End Set
    End Property

    'Public Property Album As String
    Public Shared ReadOnly AlbumProperty As DependencyProperty = DependencyProperty.Register("Album",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Album() As String
        Get
            Return DirectCast(GetValue(AlbumProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(AlbumProperty, value)
        End Set
    End Property

    'Public Property Groupement As String
    Public Shared ReadOnly GroupementProperty As DependencyProperty = DependencyProperty.Register("Groupement",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Groupement() As String
        Get
            Return DirectCast(GetValue(GroupementProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(GroupementProperty, value)
        End Set
    End Property

    'Public Property Compositeur As String
    Public Shared ReadOnly CompositeurProperty As DependencyProperty = DependencyProperty.Register("Compositeur",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Compositeur() As String
        Get
            Return DirectCast(GetValue(CompositeurProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(CompositeurProperty, value)
        End Set
    End Property

    'Public Property Annee As String
    Public Shared ReadOnly AnneeProperty As DependencyProperty = DependencyProperty.Register("Annee",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Annee() As String
        Get
            Return DirectCast(GetValue(AnneeProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(AnneeProperty, value)
        End Set
    End Property

    'Public Property AnneeOrigine As String
    Public Shared ReadOnly AnneeOrigineProperty As DependencyProperty = DependencyProperty.Register("AnneeOrigine",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property AnneeOrigine() As String
        Get
            Return DirectCast(GetValue(AnneeOrigineProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(AnneeOrigineProperty, value)
        End Set
    End Property

    'Public Property Piste As String
    Public Shared ReadOnly PisteProperty As DependencyProperty = DependencyProperty.Register("Piste",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Piste() As String
        Get
            Return DirectCast(GetValue(PisteProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(PisteProperty, value)
        End Set
    End Property

    'Public Property PisteTotale As String
    Public Shared ReadOnly PisteTotaleProperty As DependencyProperty = DependencyProperty.Register("PisteTotale",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property PisteTotale() As String
        Get
            Return DirectCast(GetValue(PisteTotaleProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(PisteTotaleProperty, value)
        End Set
    End Property

    'Public Property Disque As String
    Public Shared ReadOnly DisqueProperty As DependencyProperty = DependencyProperty.Register("Disque",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Disque() As String
        Get
            Return DirectCast(GetValue(DisqueProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(DisqueProperty, value)
        End Set
    End Property

    'Public Property DisqueTotal As String
    Public Shared ReadOnly DisqueTotalProperty As DependencyProperty = DependencyProperty.Register("DisqueTotal",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property DisqueTotal() As String
        Get
            Return DirectCast(GetValue(DisqueTotalProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(DisqueTotalProperty, value)
        End Set
    End Property

    'Public Property Bpm As String
    Public Shared ReadOnly BpmProperty As DependencyProperty = DependencyProperty.Register("Bpm",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Bpm() As String
        Get
            Return DirectCast(GetValue(BpmProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(BpmProperty, value)
        End Set
    End Property

    'Public Property Style As String
    Public Shared ReadOnly StyleProperty As DependencyProperty = DependencyProperty.Register("Style",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Style() As String
        Get
            Return DirectCast(GetValue(StyleProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(StyleProperty, value)
        End Set
    End Property

    'Public Property Label As String
    Public Shared ReadOnly LabelProperty As DependencyProperty = DependencyProperty.Register("Label",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Label() As String
        Get
            Return DirectCast(GetValue(LabelProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(LabelProperty, value)
        End Set
    End Property

    'Public Property Catalogue As String
    Public Shared ReadOnly CatalogueProperty As DependencyProperty = DependencyProperty.Register("Catalogue",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Catalogue() As String
        Get
            Return DirectCast(GetValue(CatalogueProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(CatalogueProperty, value)
        End Set
    End Property

    'Public Property PageWeb As String
    Public Shared ReadOnly PageWebProperty As DependencyProperty = DependencyProperty.Register("PageWeb",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property PageWeb() As String
        Get
            Return DirectCast(GetValue(PageWebProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(PageWebProperty, value)
        End Set
    End Property

    'Public Property Compilation As String
    Public Shared ReadOnly CompilationProperty As DependencyProperty = DependencyProperty.Register("Compilation",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Compilation() As Boolean
        Get
            Return DirectCast(GetValue(CompilationProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(CompilationProperty, value)
        End Set
    End Property

    'Public Property Commentaire As String
    Public Shared ReadOnly CommentaireProperty As DependencyProperty = DependencyProperty.Register("Commentaire",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Commentaire() As String
        Get
            Return DirectCast(GetValue(CommentaireProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(CommentaireProperty, value)
        End Set
    End Property

    'Public Property Image As ImageSource
    Public Shared ReadOnly ImageProperty As DependencyProperty = DependencyProperty.Register("Image",
        GetType(ImageSource), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property Image() As ImageSource
        Get
            Return DirectCast(GetValue(ImageProperty), ImageSource)
        End Get
        Set(ByVal value As ImageSource)
            SetValue(ImageProperty, value)
        End Set
    End Property

    'Public Property ImageLecteur As ImageSource
    Public Shared ReadOnly ImageLecteurProperty As DependencyProperty = DependencyProperty.Register("ImageLecteur",
        GetType(ImageSource), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property ImageLecteur() As ImageSource
        Get
            Return DirectCast(GetValue(ImageLecteurProperty), ImageSource)
        End Get
        Set(ByVal value As ImageSource)
            SetValue(ImageLecteurProperty, value)
        End Set
    End Property

    'Public Property ImageDosPochette As ImageSource
    Public Shared ReadOnly ImageDosPochetteProperty As DependencyProperty = DependencyProperty.Register("ImageDosPochette",
        GetType(ImageSource), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property ImageDosPochette() As ImageSource
        Get
            Return DirectCast(GetValue(ImageDosPochetteProperty), ImageSource)
        End Get
        Set(ByVal value As ImageSource)
            SetValue(ImageDosPochetteProperty, value)
        End Set
    End Property

    'Public Property ImageLabel As ImageSource
    Public Shared ReadOnly ImageLabelProperty As DependencyProperty = DependencyProperty.Register("ImageLabel",
        GetType(ImageSource), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property ImageLabel() As ImageSource
        Get
            Return DirectCast(GetValue(ImageLabelProperty), ImageSource)
        End Get
        Set(ByVal value As ImageSource)
            SetValue(ImageLabelProperty, value)
        End Set
    End Property

    'Public Property HauteurImage As Integer
    Public Shared ReadOnly HauteurImageProperty As DependencyProperty = DependencyProperty.Register("HauteurImage",
        GetType(Integer), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property HauteurImage() As Integer
        Get
            Return DirectCast(GetValue(HauteurImageProperty), Integer)
        End Get
        Set(ByVal value As Integer)
            SetValue(HauteurImageProperty, value)
        End Set
    End Property

    'Public Property LargeurImage As Integer
    Public Shared ReadOnly LargeurImageProperty As DependencyProperty = DependencyProperty.Register("LargeurImage",
        GetType(Integer), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property LargeurImage() As Integer
        Get
            Return DirectCast(GetValue(LargeurImageProperty), Integer)
        End Get
        Set(ByVal value As Integer)
            SetValue(LargeurImageProperty, value)
        End Set
    End Property

    'Public Property Padding As Integer
    Public Shared ReadOnly PaddingProperty As DependencyProperty = DependencyProperty.Register("Padding",
        GetType(Integer), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(0, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Padding() As Integer
        Get
            Return DirectCast(GetValue(PaddingProperty), Integer)
        End Get
        Set(ByVal value As Integer)
            SetValue(PaddingProperty, value)
        End Set
    End Property

    'Public Property Duree As String
    Public Shared ReadOnly DureeProperty As DependencyProperty = DependencyProperty.Register("Duree",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property Duree() As String
        Get
            Return DirectCast(GetValue(DureeProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(DureeProperty, value)
        End Set
    End Property

    'Public Property idRelease As String
    Public Shared ReadOnly idReleaseProperty As DependencyProperty = DependencyProperty.Register("idRelease",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property idRelease() As String
        Get
            Return DirectCast(GetValue(idReleaseProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(idReleaseProperty, value)
        End Set
    End Property

    'Public Property idImage As String
    Public Shared ReadOnly idImageProperty As DependencyProperty = DependencyProperty.Register("idImage",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata("", New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property idImage() As String
        Get
            Return DirectCast(GetValue(idImageProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(idImageProperty, value)
        End Set
    End Property

    'Public Property VinylCollection As Boolean
    Public Shared ReadOnly VinylCollectionProperty As DependencyProperty = DependencyProperty.Register("VinylCollection",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property VinylCollection() As Boolean
        Get
            Return DirectCast(GetValue(VinylCollectionProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(VinylCollectionProperty, value)
        End Set
    End Property

    'Public Property VinylDiscogs As Boolean
    Public Shared ReadOnly VinylDiscogsProperty As DependencyProperty = DependencyProperty.Register("VinylDiscogs",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property VinylDiscogs() As Boolean
        Get
            Return DirectCast(GetValue(VinylDiscogsProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(VinylDiscogsProperty, value)
        End Set
    End Property

    'Public Property VinylWanted As Boolean
    Public Shared ReadOnly VinylWantedProperty As DependencyProperty = DependencyProperty.Register("VinylWanted",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property VinylWanted() As Boolean
        Get
            Return DirectCast(GetValue(VinylWantedProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(VinylWantedProperty, value)
        End Set
    End Property

    'Public Property VinylEquivalent As Boolean
    Public Shared ReadOnly VinylEquivalentProperty As DependencyProperty = DependencyProperty.Register("VinylEquivalent",
        GetType(Boolean), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(False, New PropertyChangedCallback(AddressOf MemoriseModification)))
    Public Property VinylEquivalent() As Boolean
        Get
            Return DirectCast(GetValue(VinylEquivalentProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(VinylEquivalentProperty, value)
        End Set
    End Property

    'Public Property Bitrate As String
    Public Shared ReadOnly BitrateProperty As DependencyProperty = DependencyProperty.Register("Bitrate",
        GetType(String), GetType(tagID3FilesInfosDO), New UIPropertyMetadata(Nothing))
    Public Property Bitrate() As String
        Get
            Return DirectCast(GetValue(BitrateProperty), String)
        End Get
        Set(ByVal value As String)
            SetValue(BitrateProperty, value)
        End Set
    End Property

    Private FenetreParente As MainWindow
    Private _Selected As Boolean
    Public Structure MemModifications
        Public Prop As String
        Public NewValue As Object
        Public OldValue As Object
        Public NonValide As Boolean
        Public Sub New(ByVal NomProp As String, ByVal Valeur As Object, ByVal AncValeur As Object)
            Prop = NomProp
            NewValue = Valeur
            OldValue = AncValeur
        End Sub
        Public Sub New(ByVal ObjetACloner As MemModifications)
            Prop = ObjetACloner.Prop
            NewValue = ObjetACloner.NewValue
            OldValue = ObjetACloner.OldValue
            NonValide = ObjetACloner.NonValide
        End Sub
    End Structure

    Public Sub New()
    End Sub
    Public Sub New(ByVal FilesInfosOriginal As tagID3FilesInfos) 'CLONAGE
        Try
            Update(FilesInfosOriginal, CType(Application.Current.MainWindow, MainWindow))
        Catch ex As Exception
            Debug.Print("Erreur tagID3FilesInfosDO.New Partie clonage - Fenetre MainWindow inexistante")
        End Try
    End Sub
    Public Sub New(ByVal FilesInfosOriginal As tagID3FilesInfos, ByVal Fenetre As MainWindow) 'CLONAGE
        Update(FilesInfosOriginal, Fenetre)
    End Sub
    Public Sub Update(ByVal FilesInfosOriginal As tagID3FilesInfos, Optional ByVal fenetre As MainWindow = Nothing) 'CLONAGE
        '  If FilesInfosOriginal.Nom <> "" Then
        ListeFichiers = FilesInfosOriginal.ListeFichiers
        If fenetre IsNot Nothing Then FenetreParente = fenetre
        UpdateEnCours = True
        Id3v2Tag = FilesInfosOriginal.Id3v2Tag
        Id3v1Tag = FilesInfosOriginal.Id3v1Tag
        TagNormalise = FilesInfosOriginal.TagNormalise
        Nom = FilesInfosOriginal.Nom
        Extension = FilesInfosOriginal.Extension
        _NomOrigine = Nom
        Taille = FilesInfosOriginal.Taille
        Repertoire = FilesInfosOriginal.Repertoire

        FileReadOnly = FilesInfosOriginal.FileReadOnly

        Artiste = FilesInfosOriginal.Artiste
        Titre = FilesInfosOriginal.Titre
        SousTitre = FilesInfosOriginal.SousTitre
        Album = FilesInfosOriginal.Album
        Groupement = FilesInfosOriginal.Groupement
        Compositeur = FilesInfosOriginal.Compositeur
        Annee = FilesInfosOriginal.Annee
        AnneeOrigine = FilesInfosOriginal.AnneeOrigine
        Piste = FilesInfosOriginal.Piste
        PisteTotale = FilesInfosOriginal.PisteTotale
        Disque = FilesInfosOriginal.Disque
        DisqueTotal = FilesInfosOriginal.DisqueTotal
        Bpm = FilesInfosOriginal.Bpm
        Style = FilesInfosOriginal.Style
        Label = FilesInfosOriginal.Label
        Catalogue = FilesInfosOriginal.Catalogue
        PageWeb = FilesInfosOriginal.PageWeb
        Compilation = FilesInfosOriginal.Compilation
        Commentaire = FilesInfosOriginal.Commentaire
        Image = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(FilesInfosOriginal.DataImage)
        ImageDosPochette = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(FilesInfosOriginal.DataImageDosPochette)
        ImageLabel = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(FilesInfosOriginal.DataImageLabel)
        If FilesInfosOriginal.DataImageLabel IsNot Nothing Then
            ImageLecteur = ImageLabel
        Else
            ImageLecteur = Image
        End If
        If Image IsNot Nothing Then
            HauteurImage = Image.Height
            LargeurImage = Image.Width
        End If
        Padding = FilesInfosOriginal.Padding
        Duree = FilesInfosOriginal.Duree
        idRelease = FilesInfosOriginal.idRelease
        idimage = FilesInfosOriginal.idImage

        VinylCollection = FilesInfosOriginal.VinylCollection
        VinylDiscogs = FilesInfosOriginal.VinylDiscogs
        VinylWanted = FilesInfosOriginal.VinylWanted
        VinylEquivalent = FilesInfosOriginal.VinylEquivalent
        Bitrate = FilesInfosOriginal.Bitrate

        _PathOriginal = NomComplet
        _ListePropModifiees.Clear()
        UpdateEnCours = False
        '   End If
    End Sub

    Public Property ListeFichiers As New List(Of String)
    Public Property _ListePropModifiees As New List(Of MemModifications)
    Public Property Extension As String
    Public Property _NomOrigine As String
    Public Property _PathOriginal As String
    Public Property Selected As Boolean
        Get
            Return _Selected
        End Get
        Set(ByVal value As Boolean)
            _Selected = value
            If Not value Then UpdateInfosFichiers()
        End Set
    End Property
    Public ReadOnly Property NomComplet() As String
        Get
            Return Repertoire & "\" & _NomOrigine & "." & Extension
        End Get
    End Property

    'Procedure de mémorisation de toute les modifications
    Dim UpdateEnCours As Boolean
    '...A personnaliser pour chaque propriété influent sur d'autres propriétés...
    Private Shared Sub MemoriseModification(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim ObjetEnCours As tagID3FilesInfosDO = CType(d, tagID3FilesInfosDO)
        If ObjetEnCours.Selected And Not ObjetEnCours.UpdateEnCours And e.NewValue IsNot Nothing Then
            If ObjetEnCours.Extension <> "mp3" Then
                Select Case e.Property.ToString
                    Case "Nom"
                        If File.Exists(ObjetEnCours.Repertoire & "\" & e.NewValue & "." & ObjetEnCours.Extension) Then
                            ObjetEnCours.UpdateEnCours = True
                            ObjetEnCours.Nom = e.OldValue
                            ObjetEnCours.UpdateEnCours = False
                            Exit Sub
                        End If
                    Case Else
                        ObjetEnCours.UpdateEnCours = True
                        d.SetValue(e.Property, IIf(e.OldValue Is Nothing, "", e.OldValue))
                        ObjetEnCours.UpdateEnCours = False
                        Exit Sub
                End Select
            Else
                Select Case e.Property.ToString
                    Case "Nom"
                        If File.Exists(ObjetEnCours.Repertoire & "\" & e.NewValue & "." & ObjetEnCours.Extension) Then
                            ObjetEnCours.UpdateEnCours = True
                            ObjetEnCours.Nom = e.OldValue
                            ObjetEnCours.UpdateEnCours = False
                            Exit Sub
                        End If
                    Case "FileReadOnly"
                        ObjetEnCours.UpdateEnCours = True
                        If ObjetEnCours.ListeFichiers.Count > 0 Then
                            For Each i In ObjetEnCours.ListeFichiers
                                Dim Info = New FileInfo(i)
                                Info.IsReadOnly = ObjetEnCours.FileReadOnly
                            Next
                            Exit Sub
                        End If
                        ObjetEnCours.UpdateEnCours = False
                    Case "Label", "Catalogue", "Artiste", "Titre", "Album", "Annee", "Compositeur", "SousTitre",
                        "Groupement", "AnneeOrigine", "Piste", "PisteTotale", "Disque", "DisqueTotal", "Bpm", "Style",
                        "PageWeb", "Compilation", "Commentaire", "LabelVinyl", "CatalogueVinyl", "WebVinyl", "Duree",
                        "idRelease", "idImage" ',"VinylCollection", "VinylDiscogs", "VinylWanted", "VinylForSale"
                        If e.NewValue.ToString <> "" Then
                            If Not ObjetEnCours.Id3v2Tag Then
                                ObjetEnCours.Id3v2Tag = True
                                ObjetEnCours.Padding = 200 'fixe un padding par défaut lors de la création des tags v2
                            Else
                                If ObjetEnCours.Padding = 0 Then ObjetEnCours.Padding = 200
                            End If
                        End If
                    Case "Id3v2Tag"
                        If Not CType(e.NewValue, Boolean) Then
                            ObjetEnCours.Catalogue = ""
                            ObjetEnCours.Label = ""
                            ObjetEnCours.Padding = 0
                        End If
                End Select
            End If
            Dim AncienneValeur As Object = Nothing
            Try
                Dim PropModifiee As MemModifications = ObjetEnCours._ListePropModifiees.Single(Function(i As MemModifications)
                                                                                                   If i.Prop = e.Property.ToString Then Return True
                                                                                                   Return False
                                                                                               End Function)
                AncienneValeur = PropModifiee.OldValue
                ObjetEnCours._ListePropModifiees.Remove(PropModifiee)
            Catch ex As Exception
                '                MsgBox(ex.Message)
                AncienneValeur = e.OldValue
            End Try
            ObjetEnCours._ListePropModifiees.Add(New MemModifications(e.Property.ToString, e.NewValue, AncienneValeur)) ' e.OldValue))

            '   If TypeOf (ObjetEnCours.FenetreParente) Is MainWindow Then ObjetEnCours.FenetreParente.RefreshSort()
        End If
    End Sub

    'Procedure appelée lors de la deselection pour enregistrement des modifications
    '...A personnaliser pour chaque propriété utilisant le traitement des erreurs de saisie...
    Private Sub UpdateInfosFichiers()
        UpdateEnCours = True
        For i As Integer = _ListePropModifiees.Count - 1 To 0 Step -1
            If _ListePropModifiees.Item(i).NonValide Then
                Select Case _ListePropModifiees.Item(i).Prop
                    Case "Nom"
                        Nom = _ListePropModifiees.Item(i).OldValue
                    Case "Padding"
                        Padding = _ListePropModifiees.Item(i).OldValue
                        'Permet de remettre les anciennes valeurs en cas de validation sur les données...
                End Select
                _ListePropModifiees.Remove(_ListePropModifiees.Item(i))
            End If
        Next i
        If _ListePropModifiees.Count > 0 Then
            tagID3FilesInfosUpdate.UpdateInfosFichiers(Me)
            _NomOrigine = Nom
            _ListePropModifiees.Clear()
        End If
        UpdateEnCours = False
    End Sub

    '-----PROCEDURES SURCHARGEES DE L'INTERFACE IDataErrorInfo TRAITEMENT DE LA SAISIE-----
    Public ReadOnly Property [Error] As String Implements System.ComponentModel.IDataErrorInfo.Error
        Get
            Return Nothing
        End Get
    End Property
    '------Procedure de controle de validité, signale à l'interface le problème et mémorise l'annulation de la modification
    '...A personnaliser pour chaque propriété utilisant le traitement des erreurs de saisie...
    Default Public ReadOnly Property Item(ByVal columnName As String) As String Implements System.ComponentModel.IDataErrorInfo.Item
        Get
            Dim Retour As Boolean
            Try
                Select Case columnName
                    Case "Nom"
                        Dim TableauCar As String = "/ \ * ? < > | :"
                        Dim TabCar As String() = Split(TableauCar, " ")
                        Array.ForEach(TabCar, Sub(i As String)
                                                  If InStr(CType(Nom, String), i) > 0 Then Retour = True
                                              End Sub)
                        If Retour Then
                            Return "Les caractères '" & TableauCar & "' ne sont pas autorisés dans le nom d'un fichier"
                        End If
                    Case "Padding"
                        Dim Min = 0
                        Dim Max = 1000
                        If (Padding <> 0) And (Not Id3v2Tag) Then
                            Retour = True
                            Return "Le padding est paramétrable que si le fichier possède des TAG ID3v2"
                        End If
                        If (Padding > Max) Or (Padding < Min) Then
                            Retour = True
                            Return "La saisie doit être comprise entre " & Min & " et " & Max & "."
                        End If
                End Select
                Return String.Empty
            Catch ex As Exception
                Return String.Empty
            Finally
                If _ListePropModifiees.Count > 0 Then
                    If Retour Then
                        Dim NouvelleElement As New MemModifications(_ListePropModifiees.Item(_ListePropModifiees.Count - 1))
                        NouvelleElement.NonValide = True
                        _ListePropModifiees.Item(_ListePropModifiees.Count - 1) = NouvelleElement
                    Else
                        For i = _ListePropModifiees.Count - 2 To 0 Step -1
                            If _ListePropModifiees.Item(i).Prop = columnName And _ListePropModifiees.Item(i).NonValide Then
                                _ListePropModifiees.Remove(_ListePropModifiees(i))
                            End If
                        Next
                    End If
                End If
            End Try
        End Get
    End Property

    Public Sub NormaliserTagv2()
        _ListePropModifiees.Add(New MemModifications("Normaliser", "", ""))
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS --------------------------------------------
    '***********************************************************************************************
    Dim MenuContextuel As New ContextMenu
    Dim DiscogsInfos As New Discogs
    Public Async Function CreationMenuContextuelDynamique(ByVal NomChamp As String) As Tasks.Task(Of ContextMenu)
        MenuContextuel = New ContextMenu
        MenuContextuel.Tag = NomChamp
        Dim ListeMenu As New List(Of String) 'Libelle menu;Tag envoyé à la fonction de reponse,Nom sous menu
        Select Case NomChamp
            Case "Artiste"
                If ListeFichiers.Count > 1 Then Return Nothing
                If DiscogsInfos.release(DiscogsInfos.Get_ReleaseId(Me.PageWeb)) IsNot Nothing Then
                    ListeMenu.Add(DiscogsInfos.release.artiste.nom & ";NomArtiste;")
                End If
                ' If DiscogsInfos.GetRelease(DiscogsInfos.Get_ReleaseId(Me.PageWeb)) IsNot Nothing Then _
                'ListeMenu.Add(DiscogsInfos.release.artiste.nom & ";NomArtiste;")
            Case "Titre"
                Dim Compteur As Integer = 0
                If Me.Titre <> "" Then
                    Application.Config.filesInfos_settingTitleMenu.ForEach(Sub(i As String)
                                                                               Compteur += 1
                                                                               ListeMenu.Add(Trim(ExtraitChaine(Me.Titre, "", "[", , True)) _
                                                                                             & " " & i & ";PersNomTitre" & Compteur & ";")
                                                                           End Sub)
                    ListeMenu.Add(";;")
                End If
                If ListeFichiers.Count = 1 Then
                    If DiscogsInfos.release(DiscogsInfos.Get_ReleaseId(Me.PageWeb)) IsNot Nothing Then
                        Compteur = 0
                        For Each i In DiscogsInfos.release.pistes
                            Compteur += 1
                            ListeMenu.Add(i.nomPisteConcat & ";NomTitre" & Compteur & ";")
                        Next
                    End If
                End If
            Case "Album"
                If ListeFichiers.Count > 1 Then Return Nothing
                If DiscogsInfos.release(DiscogsInfos.Get_ReleaseId(Me.PageWeb)) IsNot Nothing Then
                    ListeMenu.Add(DiscogsInfos.release.titre & ";NomAlbum;")
                End If
            Case "Compositeur"
                If ListeFichiers.Count > 1 Then Return Nothing
                If DiscogsInfos.release(DiscogsInfos.Get_ReleaseId(Me.PageWeb)) IsNot Nothing Then
                    Dim compteur As Integer = 0
                    For Each i In DiscogsInfos.release.compositeurs
                        compteur += 1
                        ListeMenu.Add(i.nomCompositeursConcat & ";NomComp" & compteur & ";")
                    Next
                End If
                If Compositeur <> "" Then
                    Dim ListeCompo = Split(Compositeur, "/")
                    Dim ChaineCompo As String = "<>"
                    For Each i In ListeCompo
                        Dim DecCompo = Split(i, ",")
                        Dim NomCompo As String = ""
                        Dim PrenomCompo As String = ""
                        If DecCompo.Count > 1 Then
                            If Right(DecCompo(0), 1) = "." Then
                                ChaineCompo += DecCompo(0) & " " & DecCompo(1) & ", "
                            Else
                                ChaineCompo += DecCompo(1) & " " & DecCompo(0) & ", "
                            End If
                        Else
                            ChaineCompo += DecCompo(0) & ", "
                        End If
                    Next
                    ChaineCompo = Left(ChaineCompo, ChaineCompo.Length - 2)
                    ListeMenu.Add(";;")
                    ListeMenu.Add(ChaineCompo & ";NomComp;")
                End If
            Case "Image"
                If ListeFichiers.Count > 1 Then Return Nothing
                ListeMenu.Add("Ajouter image...;;ListeImagesDiscogs;ajouterimage24.png")
                ListeMenu.Add("Supprimer image...;;ListeImages;supprimerimage24.png")
        End Select
        ListeMenu.ForEach(Async Sub(i As String)
                              Dim ItemMenu As New MenuItem
                              Dim TabChaine() As String = Split(i, ";")
                              If TabChaine(0) <> "" Then
                                  If TabChaine(1) <> "" Then
                                      ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                                      ItemMenu.Name = TabChaine(1)
                                      ItemMenu.Tag = TabChaine(1)
                                  End If
                                  If TabChaine(2) <> "" Then
                                       CreationItemsDynamiques(TabChaine(2), ItemMenu.Items)
                                  End If
                                  If TabChaine.Count >= 4 Then
                                      If TabChaine(3) <> "" Then
                                          Dim ImageIcon As Image = New Image()
                                          ImageIcon.Height = 16
                                          ImageIcon.Width = 16
                                          ImageIcon.Stretch = Stretch.Fill
                                          ImageIcon.Source = Await GetBitmapImage("../Images/imgmenus/" & TabChaine(3))
                                          ItemMenu.Header = CreateMenuItemGrid(ImageIcon, 16, TabChaine(0))
                                      End If
                                  Else
                                      ItemMenu.Header = TabChaine(0)
                                  End If
                                  MenuContextuel.Items.Add(ItemMenu)
                              Else
                                  MenuContextuel.Items.Add(New Separator)
                              End If
                          End Sub)
        Return MenuContextuel
    End Function
    Private Async Sub CreationItemsDynamiques(ByVal NomChamp As String, ByVal ItemsMenu As ItemCollection, Optional ByVal Desactive As Boolean = False)
        Select Case NomChamp
            Case "ListeImages"
                Using Fichier As New gbDev.TagID3.tagID3Object(Me.NomComplet)
                    Dim Compteur As Integer = 0
                    For Each i In Fichier.ID3v2_Frames
                        If i.ID = "APIC" Then
                            Compteur += 1
                            Dim ItemMenu As New MenuItem
                            Dim ImageDisque As Image = New Image()
                            ImageDisque.Height = 60
                            ImageDisque.Width = 60
                            ImageDisque.Stretch = Stretch.Fill
                            Dim DataImage = Fichier.ID3v2_ImageData(i.Description)
                            Dim image As ImageSource = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(DataImage)
                            ImageDisque.Source = image
                            ImageDisque.Style = CType(ImageDisque.FindResource("GBImage"), Style)
                            If Desactive Then ImageDisque.Opacity = 0.5
                            Dim TexteImage As String = "Image " & IIf(i.Description = "", "par défaut", ": " & i.Description) & Chr(13) & _
                                                        "[" & image.Height.ToString("F0") & " x " & image.Width.ToString("F0") & "]"
                            ItemMenu.Header = CreateMenuItemGrid(ImageDisque, 60, TexteImage)
                            ItemMenu.Tag = TexteImage
                            ItemMenu.Name = "SupImage" & Compteur
                            If Desactive Then
                                ItemMenu.IsEnabled = False
                            Else
                                ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                            End If
                            ItemsMenu.Add(ItemMenu)
                        End If
                    Next
                End Using
            Case "ListeImagesDiscogs"
                If DiscogsInfos.release(idRelease) IsNot Nothing Then
                    Dim Compteur As Integer = 0
                    For Each i In DiscogsInfos.release.images
                        Dim Chaine = i
                        Dim ItemMenu As New MenuItem
                        Dim ImageDisque As Image = New Image()
                        ImageDisque.Height = 60
                        ImageDisque.Width = 60
                        ImageDisque.Stretch = Stretch.Fill
                        ImageDisque.Source = Await GetBitmapImage(i.urlImage150)
                        ImageDisque.Style = CType(ImageDisque.FindResource("GBImage"), Style)
                        ItemMenu.Header = CreateMenuItemGrid(ImageDisque, 60, "Image discogs :" & Chr(13) & "[" & i.hauteur & " x " & i.largeur & "]")
                        Dim SousItem As New MenuItem
                        SousItem.Header = "Image par défaut"
                        SousItem.Tag = Compteur
                        SousItem.Name = "AddImageDefaut" & Compteur
                        SousItem.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                        ItemMenu.Items.Add(SousItem)
                        Dim SousItem1 As New MenuItem
                        SousItem1.Header = "Image Dos pochette"
                        SousItem1.Tag = Compteur
                        SousItem1.Name = "AddImageDosPochette" & Compteur
                        SousItem1.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                        ItemMenu.Items.Add(SousItem1)
                        Dim SousItem2 As New MenuItem
                        SousItem2.Header = "Image Label"
                        SousItem2.Tag = Compteur
                        SousItem2.Name = "AddImageLabel" & Compteur
                        SousItem2.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                        ItemMenu.Items.Add(SousItem2)
                        ItemsMenu.Add(ItemMenu)
                        Compteur += 1
                    Next
                End If
                ItemsMenu.Add(New Separator)
                CreationItemsDynamiques("ListeImages", ItemsMenu, True)
        End Select
        Return
     End Sub
    Public Async Function CreationMenuImagesDiscogs(ByVal NomChamp As String, ByVal ObjetProprietaire As UIElement) As Tasks.Task(Of ContextMenu)
        MenuContextuel = New ContextMenu
        MenuContextuel.Tag = NomChamp
        If DiscogsInfos.release(idRelease) IsNot Nothing Then
            Dim Compteur As Integer = 0
            For Each i In DiscogsInfos.release.images
                Dim Chaine = i
                Dim ItemMenu As New MenuItem
                Dim ImageDisque As Image = New Image()
                ImageDisque.Height = 60
                ImageDisque.Width = 60
                ImageDisque.Stretch = Stretch.Fill
                ImageDisque.Source = Await GetBitmapImage(i.urlImage150)
                ImageDisque.Style = CType(ImageDisque.FindResource("GBImage"), Style)
                Dim TexteImage As String = "Image discogs :" & Chr(13) & "[" & i.hauteur & " x " & i.largeur & "]"
                ItemMenu.Header = CreateMenuItemGrid(ImageDisque, 60, TexteImage)
                ItemMenu.Tag = Compteur
                ItemMenu.Name = NomChamp & Compteur
                ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                MenuContextuel.Items.Add(ItemMenu)
                Compteur += 1
            Next
        End If
        MenuContextuel.PlacementTarget = ObjetProprietaire
        MenuContextuel.IsOpen = True
        Return MenuContextuel
    End Function
    Private Function CreateMenuItemGrid(ByVal Icon As Image, ByVal TailleIcon As Integer, ByVal TexteItem As String) As Grid
        Dim Grille As Grid = New Grid
        Dim Colonne1 As ColumnDefinition = New ColumnDefinition
        Colonne1.Width = New GridLength(TailleIcon)
        Grille.ColumnDefinitions.Add(Colonne1)
        Grid.SetColumn(Icon, 0)
        Dim Colonne2 As ColumnDefinition = New ColumnDefinition
        Colonne2.Width = New GridLength(14)
        Grille.ColumnDefinitions.Add(Colonne2)
        Dim Colonne3 As ColumnDefinition = New ColumnDefinition
        Grille.ColumnDefinitions.Add(Colonne3)
        Grille.Children.Add(Icon)
        Dim Texte As TextBlock = New TextBlock()
        Texte.Foreground = Brushes.White
        Texte.Text = TexteItem
        Grid.SetColumn(Texte, 2)
        Grille.Children.Add(Texte)
        Return Grille
    End Function
    Private Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If CType(e.OriginalSource, MenuItem).Name Like "PersNomTitre*" Then
            Titre = CType(e.OriginalSource, MenuItem).Header
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "NomTitre*" Then
            Titre = Trim(ExtraitChaine(CType(e.OriginalSource, MenuItem).Header, ">", "|"))
            Dim LaPiste As String = Trim(ExtraitChaine(CType(e.OriginalSource, MenuItem).Header, "<", ">"))
            'If Album <> "" Then
            Piste = LaPiste
            'End If
            If DiscogsInfos.release(DiscogsInfos.Get_ReleaseId(PageWeb)) IsNot Nothing Then
                For Each i In DiscogsInfos.release.compositeurs
                    If LaPiste = i.numPiste Then
                        If i.nomCompositeurs <> "" Then Compositeur = i.nomCompositeurs
                        Exit For
                    End If
                Next
            End If
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "NomArtiste" Then
            Artiste = CType(e.OriginalSource, MenuItem).Header
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "NomAlbum" Then
            Album = CType(e.OriginalSource, MenuItem).Header
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "NomComp*" Then
            Compositeur = Trim(ExtraitChaine(CType(e.OriginalSource, MenuItem).Header, ">", ""))
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "SupImage*" Then
            Using Fichier As New gbDev.TagID3.tagID3Object()
                Fichier.Confirmation = True
                Fichier.EnregistrementAuto = True
                Fichier.FileNameMp3 = Me.NomComplet
                Fichier.ID3v2_SetImage("", Trim(ExtraitChaine(CType(e.OriginalSource, MenuItem).Tag, ":", Chr(13))), MenuContextuel)
            End Using
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "AddImageDefaut*" Then
            Dim NumImage As Integer = CInt(CType(e.OriginalSource, MenuItem).Tag)
            Using Fichier As New gbDev.TagID3.tagID3Object()
                Fichier.Confirmation = True
                Fichier.EnregistrementAuto = True
                Fichier.FileNameMp3 = Me.NomComplet
                Fichier.ID3v2_SetImage(DiscogsInfos.release.images(NumImage).urlImage, , MenuContextuel)
            End Using
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "AddImageDosPochette*" Then
            Dim NumImage As Integer = CInt(CType(e.OriginalSource, MenuItem).Tag)
            Using Fichier As New gbDev.TagID3.tagID3Object()
                Fichier.Confirmation = True
                Fichier.EnregistrementAuto = True
                Fichier.FileNameMp3 = Me.NomComplet
                Fichier.ID3v2_SetImage(DiscogsInfos.release.images(NumImage).urlImage, "Dos pochette", MenuContextuel)
            End Using
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "AddImageLabel*" Then
            Dim NumImage As Integer = CInt(CType(e.OriginalSource, MenuItem).Tag)
            Using Fichier As New gbDev.TagID3.tagID3Object()
                Fichier.Confirmation = True
                Fichier.EnregistrementAuto = True
                Fichier.FileNameMp3 = Me.NomComplet
                Fichier.ID3v2_SetImage(DiscogsInfos.release.images(NumImage).urlImage, "Label", MenuContextuel)
            End Using
        End If
    End Sub
    Public Sub SuppressionImage(ByVal Description As String, Optional ByVal ElementParent As UIElement = Nothing)
        Using Fichier As New gbDev.TagID3.tagID3Object()
            Fichier.Confirmation = True
            Fichier.EnregistrementAuto = True
            Fichier.FileNameMp3 = Me.NomComplet
            Fichier.ID3v2_SetImage("", Description, ElementParent)
        End Using

    End Sub
    Public Sub ExtractionInfosTitre()
        Using Fichier As New gbDev.TagID3.tagID3Object(NomComplet)
            'Dim DataConfig As ConfigPerso = ConfigPerso.LoadConfig
            Dim ChaineExtractionInfos As String = Application.Config.filesInfos_stringFormat_extractInfos
            If ChaineExtractionInfos <> "" Then
                Fichier.ExtractInfosTitre(ChaineExtractionInfos)
                Artiste = Fichier.ID3v2_Texte("TPE1")
                Titre = Fichier.ID3v2_Texte("TIT2")
                Piste = ExtraitChaine(Fichier.ID3v2_Texte("TRCK"), "", "/")
                Annee = Fichier.ID3v2_Texte("TYER")
                Album = Fichier.ID3v2_Texte("TALB")
                Catalogue = Fichier.ID3v2_Texte("TIT3")
            End If
        End Using
    End Sub
    Private Async Function GetBitmapImage(ByVal NomImage As String) As Tasks.Task(Of ImageSource)
        Dim bi3 As New BitmapImage
        Try
            Dim UriFileName As New Uri(NomImage, UriKind.RelativeOrAbsolute)
            If UriFileName.IsAbsoluteUri Then
                If (UriFileName.Scheme = Uri.UriSchemeFile) Then
                    bi3.BeginInit()
                    bi3.UriSource = UriFileName
                    bi3.EndInit()
                ElseIf UriFileName.Scheme = Uri.UriSchemeHttp Then
                    bi3 = New BitmapImage
                    bi3.BeginInit()
                    Dim WebClient As New System.Net.WebClient
                    WebClient.Headers.Add("user-agent", "GBPlayer3")
                    Dim Data As Byte() = Await WebClient.DownloadDataTaskAsync(UriFileName)
                    bi3.StreamSource = New MemoryStream(Data)
                    bi3.EndInit()
                End If
            Else
                bi3.BeginInit()
                bi3.UriSource = UriFileName
                bi3.EndInit()
            End If
        Catch ex As Exception
        End Try
        Return bi3
    End Function

    '***********************************************************************************************
    '-----PROCEDURES SURCHARGEES DE L'INTERFACE ISerializable pour le drag & drop-----
    '***********************************************************************************************
    Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        If info Is Nothing Then
            Throw New System.ArgumentNullException("info")
        End If
        Id3v2Tag = CBool(info.GetValue("Id3v2Tag", GetType(Boolean)))
        Id3v1Tag = CBool(info.GetValue("Id3v1Tag", GetType(Boolean)))
        Nom = info.GetString("Nom")
        Extension = info.GetString("Extension")
        Taille = info.GetInt32("Taille")
        Repertoire = info.GetString("Repertoire")
        Artiste = info.GetString("Artiste")
        Titre = info.GetString("Titre")
        SousTitre = info.GetString("SousTitre")
        Album = info.GetString("Album")
        Groupement = info.GetString("Groupement")
        Compositeur = info.GetString("Compositeur")
        Annee = info.GetString("Annee")
        AnneeOrigine = info.GetString("AnneeOrigine")
        Piste = info.GetString("Piste")
        PisteTotale = info.GetString("PisteTotale")
        Disque = info.GetString("Disque")
        DisqueTotal = info.GetString("DisqueTotal")
        Bpm = info.GetString("Bpm")
        Style = info.GetString("Style")
        Label = info.GetString("Label")
        Catalogue = info.GetString("Catalogue")
        PageWeb = info.GetString("PageWeb")
        Compilation = CBool(info.GetValue("Compilation", GetType(Boolean)))
        Commentaire = info.GetString("Commentaire")
        Padding = info.GetInt32("Padding")
        Duree = info.GetString("Duree")
        idRelease = info.GetString("idRelease")

        VinylCollection = info.GetBoolean("VinylCollection")
        VinylDiscogs = info.GetBoolean("VinylDiscogs")
        VinylWanted = info.GetBoolean("VinylWanted")
        VinylEquivalent = info.GetBoolean("VinylEquivalent")
        Bitrate = info.GetString("Bitrate")

    End Sub
    Public Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
        info.AddValue("Id3v2Tag", Id3v2Tag)
        info.AddValue("Id3v1Tag", Id3v1Tag)
        info.AddValue("Nom", Nom)
        info.AddValue("Extension", Extension)
        info.AddValue("Taille", Taille)
        info.AddValue("Repertoire", Repertoire)
        info.AddValue("Artiste", Artiste)
        info.AddValue("Titre", Titre)
        info.AddValue("SousTitre", SousTitre)
        info.AddValue("Album", Album)
        info.AddValue("Groupement", Groupement)
        info.AddValue("Compositeur", Compositeur)
        info.AddValue("Annee", Annee)
        info.AddValue("AnneeOrigine", AnneeOrigine)
        info.AddValue("Piste", Piste)
        info.AddValue("PisteTotale", PisteTotale)
        info.AddValue("Disque", Disque)
        info.AddValue("DisqueTotal", DisqueTotal)
        info.AddValue("Bpm", Bpm)
        info.AddValue("Style", Style)
        info.AddValue("Label", Label)
        info.AddValue("Catalogue", Catalogue)
        info.AddValue("PageWeb", PageWeb)
        info.AddValue("Compilation", Compilation)
        info.AddValue("Commentaire", Commentaire)
        info.AddValue("Padding", Padding)
        info.AddValue("Duree", Duree)
        info.AddValue("idRelease", idRelease)

        info.AddValue("VinylCollection", VinylCollection)
        info.AddValue("VinylDiscogs", VinylDiscogs)
        info.AddValue("VinylWanted", VinylWanted)
        info.AddValue("VinylEquivalent", VinylEquivalent)
        info.AddValue("Bitrate", Bitrate)

    End Sub
End Class
