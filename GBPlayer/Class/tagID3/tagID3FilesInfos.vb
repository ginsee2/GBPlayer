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
'-------------------------------CLASSE DE STOCKAGE INTERMEDIAIRE POUR PASSER ENTRE THREADS------
'***********************************************************************************************
Public Class tagID3FilesInfos
    Inherits DependencyObject
    Public Property ListeFichiers As New List(Of String)
    Public Property Id3v2Tag As Boolean
    Public Property Id3v1Tag As Boolean
    Public Property TagNormalise As Boolean
    Public Property Nom As String
    Public Property Extension As String
    Public Property _NomOrigine As String
    Public Property Taille As Integer
    Public Property Repertoire As String
    Public Property FileReadOnly As Boolean
    Public Property Artiste As String
    Public Property Titre As String
    Public Property SousTitre As String
    Public Property Album As String
    Public Property Groupement As String
    Public Property Compositeur As String
    Public Property Annee As String
    Public Property AnneeOrigine As String
    Public Property Piste As String
    Public Property PisteTotale As String
    Public Property Disque As String
    Public Property DisqueTotal As String
    Public Property Bpm As String
    Public Property Style As String
    Public Property Label As String
    Public Property Catalogue As String
    Public Property PageWeb As String
    Public Property Compilation As Boolean
    Public Property Commentaire As String
    Public Property Image As ImageSource
    Public Property DataImage As Byte()
    Public Property DataImageDosPochette As Byte()
    Public Property DataImageLabel As Byte()
    Public Property HauteurImage As Integer
    Public Property LargeurImage As Integer
    Public Property Padding As Integer

    Public Property Duree As String
    Public Property idRelease As String
    Public Property idImage As String

    Public Property VinylCollection As Boolean
    Public Property VinylDiscogs As Boolean
    Public Property VinylWanted As Boolean
    Public Property VinylEquivalent As Boolean
    Public Property Bitrate As String

    Public Property TabModif As New List(Of String)

    Public Property NomComplet As String
    Public Property Tag As Object

    Public Sub New(ByVal Fichier As XElement)
        Try
            Nom = Fichier.<NomFichier>.Value
            Repertoire = Fichier.<Repertoire>.Value
            Extension = Fichier.<Extension>.Value
            ListeFichiers.Add(Fichier.<Nom>.Value)
            Taille = Fichier.<Taille>.Value

            Id3v2Tag = CBool(Fichier.<Id3v2Tag>.Value)
            Id3v1Tag = CBool(Fichier.<Id3v1Tag>.Value)
            Artiste = Fichier.<Artiste>.Value
            Titre = Fichier.<Titre>.Value
            SousTitre = Fichier.<SousTitre>.Value
            Album = Fichier.<Album>.Value
            Groupement = Fichier.<Groupement>.Value
            Compositeur = Fichier.<Compositeur>.Value
            Annee = Fichier.<Annee>.Value
            AnneeOrigine = Fichier.<AnneOrigine>.Value
            Piste = Fichier.<Piste>.Value
            PisteTotale = Fichier.<PisteTotale>.Value
            Disque = Fichier.<Disque>.Value
            DisqueTotal = Fichier.<DisqueTotal>.Value
            Bpm = Fichier.<Bpm>.Value
            Style = Fichier.<Style>.Value
            Label = Fichier.<Label>.Value
            Catalogue = Fichier.<Catalogue>.Value
            PageWeb = Fichier.<PageWeb>.Value
            Compilation = CBool(Fichier.<Compilation>.Value)
            Commentaire = Fichier.<Commentaire>.Value
            Padding = Fichier.<TaillePadding>.Value
            Duree = Fichier.<Duree>.Value
            idRelease = Fichier.<idRelease>.Value
            idImage = Fichier.<idImage>.Value

            VinylCollection = Fichier.<VinylCollection>.Value
            VinylDiscogs = Fichier.<VinylDiscogs>.Value
            VinylWanted = Fichier.<VinylWanted>.Value
            VinylEquivalent = Fichier.<VinylEquivalent>.Value
            Bitrate = Fichier.<Bitrate>.Value

            'DataImage = Fichier.ID3v2_ImageData
            'Image = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(DataImage)
            'If Image IsNot Nothing Then
            'HauteurImage = Image.Height
            'LargeurImage = Image.Width
            'End If
            'DataImageDosPochette = Fichier.ID3v2_ImageData("Dos pochette")
            'DataImageLabel = Fichier.ID3v2_ImageData("Label")

            If idRelease = "" Then
                If PageWeb <> "" Then idRelease = ExtraitChaine(PageWeb, "release/", "", 8)
            End If
        Catch ex As Exception
        End Try
    End Sub
    Public Sub New(ByVal FileNamePath As String)
        Try
            Select Case Path.GetExtension(FileNamePath)
                Case ".wav"
                    Nom = GetFileName(FileNamePath)
                    Extension = GetFileExt(FileNamePath)
                    Repertoire = GetFilePath(FileNamePath)
                    Taille = New FileInfo(FileNamePath).Length.ToString
                    FileReadOnly = New FileInfo(FileNamePath).IsReadOnly
                    NomComplet = FileNamePath
                    ListeFichiers.Add(FileNamePath)
                Case ".mp3"
                    Using Fichier As New gbDev.TagID3.tagID3Object
                        Fichier.SearchPadding = False
                        Fichier.ForcageLecture = True
                        Fichier.FileNameMp3 = FileNamePath
                        Nom = GetFileName(FileNamePath)
                        Repertoire = GetFilePath(FileNamePath)
                        Extension = GetFileExt(FileNamePath)
                        If Fichier.FileNameMp3 <> "" Then
                            ListeFichiers.Add(FileNamePath)
                            Taille = New FileInfo(FileNamePath).Length.ToString

                            FileReadOnly = New FileInfo(FileNamePath).IsReadOnly
                            Dim essai = New FileInfo(FileNamePath).GetAccessControl
                            Id3v2Tag = Fichier.ID3v2_Ok
                            Id3v1Tag = Fichier.ID3v1_Ok
                            'TagNormalise = Not Fichier.NormaliseID3v2(True)
                            Artiste = Fichier.ID3v2_Texte("TPE1")
                            Titre = Fichier.ID3v2_Texte("TIT2")
                            SousTitre = Fichier.ID3v2_TextePerso("VersionTitre")
                            Album = Fichier.ID3v2_Texte("TALB")
                            Groupement = Fichier.ID3v2_Texte("TIT1")
                            Compositeur = Fichier.ID3v2_Texte("TCOM")
                            Annee = Fichier.ID3v2_Texte("TYER")
                            AnneeOrigine = Fichier.ID3v2_Texte("TORY")
                            Piste = ExtraitChaine(Fichier.ID3v2_Texte("TRCK"), "", "/")
                            PisteTotale = ExtraitChaine(Fichier.ID3v2_Texte("TRCK"), "/", "")
                            Disque = ExtraitChaine(Fichier.ID3v2_Texte("TPOS"), "", "/")
                            DisqueTotal = ExtraitChaine(Fichier.ID3v2_Texte("TPOS"), "/", "")
                            Bpm = Fichier.ID3v2_Texte("TBPM")
                            Style = Fichier.ID3v2_Texte("TCON")
                            Label = Fichier.ID3v2_Texte("TPUB")
                            Catalogue = Fichier.ID3v2_Texte("TIT3")
                            PageWeb = Fichier.ID3v2_Texte("WOAF")
                            Compilation = IIf(Fichier.ID3v2_Texte("TCMP") = "1", True, False)
                            Commentaire = Fichier.ID3v2_Commentaire()
                            DataImage = Fichier.ID3v2_ImageData
                            Image = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(DataImage)
                            If Image IsNot Nothing Then
                                HauteurImage = Image.Height
                                LargeurImage = Image.Width
                            End If
                            DataImageDosPochette = Fichier.ID3v2_ImageData("Dos pochette")
                            DataImageLabel = Fichier.ID3v2_ImageData("Label")

                            Padding = Fichier.ID3v2_EXTHEADER_PaddingSize
                            Duree = Fichier.ID3v2_Texte("TLEN")
                            idRelease = Fichier.ID3v2_TextePerso("idRelease")
                            idRelease = ExtraitChaine(PageWeb, "/release/", "", 9)

                            VinylCollection = False
                            VinylDiscogs = False
                            VinylWanted = False
                            VinylEquivalent = False
                            Bitrate = Fichier.ID3v2_TextePerso("Bitrate")

                            NomComplet = FileNamePath
                            If Not Duree Like "*:*" Or Bitrate = "" Then
                                Try
                                    Dim i As fileMp3 = New fileMp3 ' GBPlayerLight.clsmp3file = New GBPlayerLight.clsmp3file()
                                    i.OpenFile(NomComplet, True)
                                    Dim WaveOctetParSec = i.WaveOctetParSec
                                    Dim DureeTotale = i.PlayDureeTotale
                                    Dim MinutesT As Integer = Int(DureeTotale / WaveOctetParSec / 60)
                                    Dim SecondesT As Integer = Int((DureeTotale / WaveOctetParSec) - MinutesT * 60)
                                    Duree = String.Format("{0:D2}:{1:D2}", {MinutesT, SecondesT})
                                    Bitrate = i.GetMp3FrameHeader.Bitrate
                                    i.CloseFile()
                                Catch ex As Exception
                                End Try
                            End If
                            If idRelease = "" Then
                                If PageWeb <> "" Then idRelease = ExtraitChaine(PageWeb, "release/", "", 8)
                            End If
                        Else
                        End If
                        'NettoyageFin = Fichier.NettoyageFinMp3(True)
                    End Using
            End Select
        Catch ex As Exception
        End Try
    End Sub
    Public Sub New(ByVal FilesInfosOriginal As tagID3FilesInfosDO) 'CLONAGE
        ListeFichiers = FilesInfosOriginal.ListeFichiers
        Id3v2Tag = FilesInfosOriginal.Id3v2Tag
        Id3v1Tag = FilesInfosOriginal.Id3v1Tag
        TagNormalise = FilesInfosOriginal.TagNormalise
        Nom = FilesInfosOriginal.Nom
        Extension = FilesInfosOriginal.Extension
        Taille = FilesInfosOriginal.Taille
        Repertoire = FilesInfosOriginal.Repertoire
        _NomOrigine = FilesInfosOriginal._NomOrigine

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
        Padding = FilesInfosOriginal.Padding
        Duree = FilesInfosOriginal.Duree
        idRelease = FilesInfosOriginal.idRelease
        idImage = FilesInfosOriginal.idimage

        VinylCollection = FilesInfosOriginal.VinylCollection
        VinylDiscogs = FilesInfosOriginal.VinylDiscogs
        VinylWanted = FilesInfosOriginal.VinylWanted
        VinylEquivalent = FilesInfosOriginal.VinylEquivalent
        Bitrate = FilesInfosOriginal.Bitrate

        NomComplet = Repertoire & "\" & _NomOrigine & "." & Extension
        FilesInfosOriginal._ListePropModifiees.ForEach(Sub(i)
                                                           TabModif.Add(i.Prop)
                                                       End Sub)
    End Sub
    Public Sub Add(ByVal FileNamePath As String)
        Try
            Select Path.GetExtension(FileNamePath)
                Case ".wav"
                    Extension = "*"
                    NomComplet = "*"
                    ListeFichiers.Clear()
                Case ".mp3"
                    If Extension <> "mp3" Then
                        Extension = "*"
                        NomComplet = "*"
                        ListeFichiers.Clear()
                    Else
                        Using Fichier As New gbDev.TagID3.tagID3Object()
                            Fichier.SearchPadding = False
                            Fichier.ForcageLecture = True
                            Fichier.FileNameMp3 = FileNamePath
                            ListeFichiers.Add(FileNamePath)
                            Id3v2Tag = SyntheseBool(Id3v2Tag, Fichier.ID3v2_Ok)
                            Id3v1Tag = SyntheseBool(Id3v1Tag, Fichier.ID3v1_Ok)
                            Nom = "*" 'GetFileName(FileNamePath)
                            Extension = "mp3" 'GetFileExt(FileNamePath)
                            Taille = -1 'New FileInfo(FileNamePath).Length.ToString
                            Repertoire = "*" 'GetFilePath(FileNamePath)

                            FileReadOnly = SyntheseBool(FileReadOnly, IIf(New FileInfo(FileNamePath).IsReadOnly, True, False))

                            Artiste = SyntheseChaine(Artiste, Fichier.ID3v2_Texte("TPE1"))
                            Titre = SyntheseChaine(Titre, Fichier.ID3v2_Texte("TIT2"))
                            SousTitre = SyntheseChaine(SousTitre, Fichier.ID3v2_TextePerso("VersionTitre"))
                            Album = SyntheseChaine(Album, Fichier.ID3v2_Texte("TALB"))
                            Groupement = SyntheseChaine(Groupement, Fichier.ID3v2_Texte("TIT1"))
                            Compositeur = SyntheseChaine(Compositeur, Fichier.ID3v2_Texte("TCOM"))
                            Annee = SyntheseChaine(Annee, Fichier.ID3v2_Texte("TYER"))
                            AnneeOrigine = SyntheseChaine(AnneeOrigine, Fichier.ID3v2_Texte("TORY"))
                            Piste = SyntheseChaine(Piste, ExtraitChaine(Fichier.ID3v2_Texte("TRCK"), "", "/"))
                            PisteTotale = SyntheseChaine(PisteTotale, ExtraitChaine(Fichier.ID3v2_Texte("TRCK"), "/", ""))
                            Disque = SyntheseChaine(Disque, ExtraitChaine(Fichier.ID3v2_Texte("TPOS"), "", "/"))
                            DisqueTotal = SyntheseChaine(DisqueTotal, ExtraitChaine(Fichier.ID3v2_Texte("TPOS"), "/", ""))
                            Bpm = SyntheseChaine(Bpm, Fichier.ID3v2_Texte("TBPM"))
                            Style = SyntheseChaine(Style, Fichier.ID3v2_Texte("TCON"))
                            Label = SyntheseChaine(Label, Fichier.ID3v2_Texte("TPUB"))
                            Catalogue = SyntheseChaine(Catalogue, Fichier.ID3v2_Texte("TIT3"))
                            PageWeb = SyntheseChaine(PageWeb, Fichier.ID3v2_Texte("WOAF"))
                            Compilation = SyntheseBool(Compilation, IIf(Fichier.ID3v2_Texte("TCMP") = "1", True, False))
                            Commentaire = SyntheseChaine(Commentaire, Fichier.ID3v2_Commentaire())

                            DataImage = Nothing ' Fichier.ID3v2_ImageData
                            Image = Nothing 'TagID3.clsTagID3.FonctionUtilite.DownLoadImage(DataImage)
                            DataImageDosPochette = Nothing
                            DataImageLabel = Nothing
                            Padding = -1 'Fichier.ID3v2_EXTHEADER_PaddingSize
                            Duree = "*"
                            idRelease = "*"

                            VinylCollection = False
                            VinylDiscogs = False
                            VinylWanted = False
                            VinylEquivalent = False
                            Bitrate = "*"

                            NomComplet = "*" 'FileNamePath
                            'NettoyageFin = Fichier.NettoyageFinMp3(True)
                        End Using
                    End If
            End Select
        Catch ex As Exception
        End Try
    End Sub
    Private Function SyntheseChaine(ByVal ChaineOrigine As String, ByVal ChaineSup As String) As String
        If ChaineOrigine = "*" Then Return "*"
        If String.Equals(ChaineOrigine, ChaineSup) Then Return ChaineOrigine Else Return "*"
    End Function
    Private Function SyntheseBool(ByVal BoolOrigine As Boolean, ByVal BoolSup As Boolean) As Boolean
        If Boolean.Equals(BoolOrigine, BoolSup) Then Return BoolOrigine Else Return True
    End Function
End Class

