Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/07/10 rev 04/08/10
'DESCRIPTION :Classe de gestion TAG ID3 v1 et v2 des fichier mp3
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO                      'objet Path et Fileinfo
Imports Microsoft.VisualBasic.FileIO   'objet filesystem pour traitement fichiers et repertoires
Imports System.Threading

Namespace TagID3
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    Public Structure ID3v2Frame
        Implements ICloneable
        Public ID As String
        Public Size As Int32
        Public DiscardIfTagAltered As Boolean
        Public DiscardIfFileAltered As Boolean
        Public LectureSeule As Boolean
        Public Compressed As Boolean
        Public Encrypted As Boolean
        Public GroupingIdentity As Boolean
        Public TextEncoding As Int32
        Public Description As String
        Public Langue As String
        Public Text As String
        Public TypeImage As Byte
        Public BinaryData() As Byte
        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim NewInstance As New ID3v2Frame
            With NewInstance
                .ID = ID
                .Size = Size
                .DiscardIfTagAltered = DiscardIfTagAltered
                .DiscardIfFileAltered = DiscardIfFileAltered
                .LectureSeule = LectureSeule
                .Compressed = Compressed
                .Encrypted = Encrypted
                .GroupingIdentity = GroupingIdentity
                .TextEncoding = TextEncoding
                .Description = Description
                .Langue = Langue
                .Text = Text
                .TypeImage = TypeImage
                .BinaryData = BinaryData
            End With
            Return NewInstance
        End Function
        Public Overrides Function ToString() As String
            Return ID
        End Function
    End Structure
    Public Structure IDTAG
        Public TAG As String             'Chaine "TAG"
        Public Titre As String          'Titre de la chanson sur 30 carateres
        Public Artiste As String        'Nom artiste de la chanson sur 30 carateres
        Public Album As String          'Titre album de la chanson sur 30 carateres
        Public Annee As String           'Annee de la chanson sur 4 carateres
        Public Commentaire As String    'Commentaires sur 30 carateres
        Public Genre As Byte                                'Genre de type BYTE suivant liste
    End Structure
    Public Structure ID3v2Header
        Public IDv2TAGPresent As Boolean    'Indique la presence du TAG ID3v2
        Public VersionMajor As Byte
        Public VersionMinor As Byte
        Public TagUnsynchronised As Boolean
        Public ExtendedHeaderUsed As Boolean
        Public TagSize As Integer
        Public TotalTagSize As Int32
        Public ExtHeader_Size As Int32
        Public ExtHeader_CRCPresent As Boolean
        Public ExtHeader_PaddingSize As Int32
        Public CRC_TotalFrameCRC As Int32
        Public PositionTrames As Int32
    End Structure
    '***********************************************************************************************
    '-------------------------------ENUMERATION PUBLIQUES-------------------------------------------
    '***********************************************************************************************
    Public Enum TypeConversion_ID3v2
        GBAU_ID3v2ID_CONV22_23 = 1
        GBAU_ID3v2ID_CONV23_22 = 2
    End Enum
    '***********************************************************************************************
    '---------------------------------CLASSES PUBLIQUES---------------------------------------------
    '***********************************************************************************************
    Class tagID3Object
        '***********************************************************************************************
        '---------------------------------INTERFACE IMPLEMENTEE-----------------------------------------
        '***********************************************************************************************
        Implements System.IDisposable

        '***********************************************************************************************
        '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
        '***********************************************************************************************
        '***********************************************************************************************
        '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
        '***********************************************************************************************
        Private FichierMp3 As New fileBinary           'Fichier systeme pour les operations sur le fichier
        Private NomFichiermp3 As String             'Nom du fichier mp3
        Private TagID3v1 As IDTAG                   'Structure stockant le tag ID3v1
        Private TagID3v2Header As New ID3v2Header   'Entete des tags ID3v2 du fichier
        Private TagID3v2Frames As New List(Of ID3v2Frame)
        Private InfosIsOk As Boolean
        Private bID3v1Modifie As Boolean
        Private bID3v2Modifie As Boolean
        Private ID3v1Present As Boolean
        Private ID3v2Present As Boolean
        Private lPaddingSizeWanted As Long = -1
        Private DataConfig As TagID3Data
        '***********************************************************************************************
        '---------------------------------CONSTRUCTEUR DE LA CLASSE-------------------------------------
        '***********************************************************************************************
        Public Sub New()
        End Sub

        Public Sub New(ByVal NomFichier As String, Optional ByVal EnregistrementAutomatique As Boolean = False)
            FileNameMp3 = NomFichier
            EnregistrementAuto = EnregistrementAutomatique
        End Sub
        '***********************************************************************************************
        '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
        '***********************************************************************************************
        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub
        '***********************************************************************************************
        '----------------------------PROPRIETES DE LA CLASSE EN LECTURE/ECRITURE------------------------
        '***********************************************************************************************
        Public Property FileNameMp3() As String
            Get
                FileNameMp3 = NomFichiermp3
            End Get
            Set(ByVal value As String)
                If (value <> NomFichiermp3) Then
                    If InfosIsOk Then CleanFrames()
                    If FileSystem.FileExists(value) Then
                        InfosIsOk = False
                        If value <> "" Then If ReadFile(value, ForcageLecture) Then InfosIsOk = True
                    End If
                End If
            End Set
        End Property
        Public Property ID3v1Modifie() As Boolean
            Get
                ID3v1Modifie = bID3v1Modifie
            End Get
            Set(ByVal value As Boolean)
                If Not ForcageLecture Then bID3v1Modifie = value
            End Set
        End Property
        Public Property ID3v2Modifie() As Boolean
            Get
                ID3v2Modifie = bID3v2Modifie
            End Get
            Set(ByVal value As Boolean)
                If Not ForcageLecture Then bID3v2Modifie = value
            End Set
        End Property
        Public Property ForcageLecture() As Boolean = False
        Public Property EnregistrementAuto() As Boolean = False
        Public Property Confirmation() As Boolean = False
        Public Property SearchPadding As Boolean = True
        Private Shared LockOpen As Object = New Object

        '***********************************************************************************************
        '----------------------------LECTURE DES PROPRIETES DE LA CLASSE--------------------------------
        '***********************************************************************************************
        '--------------------------PROPRIETES INFOS SUR LE FICHIER--------------------------------------
        Public ReadOnly Property FileSize() As Long
            Get
                Dim InfoFichier As FileInfo
                If FileNameMp3 <> "" Then
                    InfoFichier = New FileInfo(FileNameMp3)
                    FileSize = InfoFichier.Length
                Else
                    FileSize = 0
                End If
            End Get
        End Property
        Public ReadOnly Property DateCreation() As String
            Get
                Dim InfoFichier As FileInfo
                If FileNameMp3 <> "" Then
                    InfoFichier = New FileInfo(FileNameMp3)
                    DateCreation = InfoFichier.CreationTime.ToString
                Else
                    DateCreation = ""
                End If
            End Get
        End Property
        Public ReadOnly Property DateModification() As String
            Get
                Dim InfoFichier As FileInfo
                If FileNameMp3 <> "" Then
                    InfoFichier = New FileInfo(FileNameMp3)
                    DateModification = InfoFichier.LastWriteTime.ToString
                Else
                    DateModification = ""
                End If
            End Get
        End Property
        Public ReadOnly Property DateDernierAcces() As String
            Get
                Dim InfoFichier As FileInfo
                If FileNameMp3 <> "" Then
                    InfoFichier = New FileInfo(FileNameMp3)
                    DateDernierAcces = InfoFichier.LastAccessTime.ToString
                Else
                    DateDernierAcces = ""
                End If
            End Get
        End Property

        '--------------------------PROPRIETES ID3V1-----------------------------------------------------
        Public Property ID3v1_Ok() As Boolean
            Get
                If TagID3v1.TAG = "TAG" Then ID3v1_Ok = True Else ID3v1_Ok = False
            End Get
            Set(ByVal Present As Boolean)
                If Present Then TagID3v1.TAG = "TAG" Else TagID3v1.TAG = ""
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Titre() As String
            Get
                If ID3v1_Ok Then ID3v1_Titre = Trim(TagID3v1.Titre.ToString) Else ID3v1_Titre = ""
            End Get
            Set(ByVal Titre As String)
                TagID3v1.Titre = Titre
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Artiste() As String
            Get
                If ID3v1_Ok Then ID3v1_Artiste = Trim(TagID3v1.Artiste.ToString) Else ID3v1_Artiste = ""
            End Get
            Set(ByVal Artiste As String)
                TagID3v1.Artiste = Artiste
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Album() As String
            Get
                If ID3v1_Ok Then ID3v1_Album = Trim(TagID3v1.Album.ToString) Else ID3v1_Album = ""
            End Get
            Set(ByVal Album As String)
                TagID3v1.Album = Album
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Annee() As String
            Get
                If ID3v1_Ok Then ID3v1_Annee = Trim(TagID3v1.Annee.ToString) Else ID3v1_Annee = ""
            End Get
            Set(ByVal Annee As String)
                TagID3v1.Annee = Annee
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Genre() As String
            Get
                If ID3v1_Ok Then ID3v1_Genre = Trim(TagID3v1.Genre.ToString) Else ID3v1_Genre = ""
            End Get
            Set(ByVal Genre As String)
                TagID3v1.Genre = Val(Genre)
                ID3v1Modifie = True
            End Set
        End Property
        Public Property ID3v1_Commentaire() As String
            Get
                If ID3v1_Ok Then ID3v1_Commentaire = Trim(TagID3v1.Commentaire.ToString) Else ID3v1_Commentaire = ""
            End Get
            Set(ByVal Commentaire As String)
                TagID3v1.Commentaire = Commentaire
                ID3v1Modifie = True
            End Set
        End Property

        '--------------------------PROPRIETES HEADER ID3V2----------------------------------------------
        Public ReadOnly Property ID3v2_Ok() As Boolean
            Get
                If TagID3v2Header.IDv2TAGPresent Then ID3v2_Ok = True Else ID3v2_Ok = False
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_VersionMajor() As Long
            Get
                ID3v2_HEADER_VersionMajor = TagID3v2Header.VersionMajor
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_VersionMinor() As Long
            Get
                ID3v2_HEADER_VersionMinor = TagID3v2Header.VersionMinor
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_UnsynchronisationUsed() As Boolean
            Get
                ID3v2_HEADER_UnsynchronisationUsed = TagID3v2Header.TagUnsynchronised
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_ExtendedHeaderUsed() As Boolean
            Get
                ID3v2_HEADER_ExtendedHeaderUsed = TagID3v2Header.ExtendedHeaderUsed
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_TagSize() As Long
            Get
                ID3v2_HEADER_TagSize = TagID3v2Header.TagSize
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_TotalTagSize() As Long
            Get
                ID3v2_HEADER_TotalTagSize = TagID3v2Header.TotalTagSize
            End Get
        End Property
        Public ReadOnly Property ID3v2_HEADER_PositionsFrames() As Long
            Get
                ID3v2_HEADER_PositionsFrames = TagID3v2Header.PositionTrames
            End Get
        End Property
        Public ReadOnly Property ID3v2_EXTHEADER_Size() As Long
            Get
                ID3v2_EXTHEADER_Size = TagID3v2Header.ExtHeader_Size
            End Get
        End Property
        Public ReadOnly Property ID3v2_EXTHEADER_CRCPresent() As Boolean
            Get
                ID3v2_EXTHEADER_CRCPresent = TagID3v2Header.ExtHeader_CRCPresent
            End Get
        End Property
        Public ReadOnly Property ID3v2_EXTHEADER_TotalFrameCRC() As Long
            Get
                ID3v2_EXTHEADER_TotalFrameCRC = TagID3v2Header.CRC_TotalFrameCRC
            End Get
        End Property
        Public Property ID3v2_EXTHEADER_PaddingSize() As Long
            Get
                ID3v2_EXTHEADER_PaddingSize = TagID3v2Header.ExtHeader_PaddingSize
            End Get
            Set(ByVal value As Long)
                If value <> TagID3v2Header.ExtHeader_PaddingSize Then
                    lPaddingSizeWanted = value
                    bID3v2Modifie = True
                End If
                If value > 0 Then TagID3v2Header.ExtendedHeaderUsed = True Else TagID3v2Header.ExtendedHeaderUsed = False
            End Set
        End Property

        '--------------------------PROPRIETES ID3V2-----------------------------------------------------
        Public Property ID3v2_Frames() As List(Of ID3v2Frame)
            Get
                Dim NewFrame As ID3v2Frame
                Dim NewFrames As New List(Of ID3v2Frame)
                TagID3v2Frames.ForEach(Sub(Frame)
                                           NewFrame = Frame
                                           NewFrames.Add(NewFrame)
                                       End Sub)
                ID3v2_Frames = NewFrames
            End Get
            Set(ByVal NewFrames As List(Of ID3v2Frame))
                Dim NewFrame As ID3v2Frame
                Dim CloneNewFrames As New List(Of ID3v2Frame)
                If Not ID3v2_Ok Then
                    TagID3v2Header.IDv2TAGPresent = True
                    TagID3v2Header.VersionMajor = 3
                End If
                If NewFrames IsNot Nothing Then NewFrames.ForEach(Sub(Frame)
                                                                      NewFrame = Frame
                                                                      CloneNewFrames.Add(NewFrame)
                                                                  End Sub) Else TagID3v2Header.IDv2TAGPresent = False
                'modif faite pour suppression des TAG v2
                ID3v2Modifie = True
                TagID3v2Frames = CloneNewFrames
            End Set
        End Property
        Public Property ID3v2_Texte(ByVal code As String) As String
            Get
                ID3v2_Texte = ""
                If ID3v2_Ok Then
                    TagID3v2Frames.ForEach(Sub(Element)
                                               If Element.ID = code Then
                                                   ID3v2_Texte = Element.Text
                                                   Exit Sub
                                               End If
                                           End Sub)
                End If
            End Get
            Set(ByVal Texte As String)
                Dim NewFrame As New ID3v2Frame
                If ID3v2_Ok Then
                    Dim FrameRecherchee = From Element In TagID3v2Frames
                                            Where Element.ID = code
                                            Select Element
                    If FrameRecherchee.Count > 0 Then
                        Dim OldFrame As ID3v2Frame = FrameRecherchee(0)
                        Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte(OldFrame.Text, Texte)
                        If Texte <> "" And Texte = OldFrame.Text Then Exit Property
                        ID3v2Modifie = True
                        TagID3v2Frames.Remove(OldFrame)
                        If Texte = "" Then Exit Property
                    End If
                Else
                    TagID3v2Header.IDv2TAGPresent = True
                    TagID3v2Header.VersionMajor = 3
                End If
                If Texte <> "" Then                 ' Ajout du nouvel élément
                    Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte("", Texte)
                    ID3v2Modifie = True
                    NewFrame.ID = code
                    NewFrame.Text = Texte
                    NewFrame.Size = Len(Texte)
                    TagID3v2Frames.Add(NewFrame)
                End If
            End Set
        End Property
        Public Property ID3v2_TextePerso(Optional ByVal Description As String = "") As String
            Get
                ID3v2_TextePerso = ""
                If ID3v2_Ok Then
                    TagID3v2Frames.ForEach(Sub(Element)
                                               If Element.ID = "TXXX" And Element.Description = Description Then
                                                   ID3v2_TextePerso = Element.Text
                                                   Exit Sub
                                               End If
                                           End Sub)
                End If
            End Get
            Set(ByVal Texte As String)
                Dim NewFrame As New ID3v2Frame
                If ID3v2_Ok Then
                    Dim FrameRecherchee = From Element In TagID3v2Frames
                                            Where Element.ID = "TXXX" And Element.Description = Description
                                            Select Element
                    If FrameRecherchee.Count > 0 Then
                        Dim OldFrame As ID3v2Frame = FrameRecherchee(0)
                        Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte(OldFrame.Text, Texte)
                        If Texte <> "" And Texte = OldFrame.Text Then Exit Property
                        ID3v2Modifie = True
                        TagID3v2Frames.Remove(OldFrame)
                        If Texte = "" Then Exit Property
                    End If
                Else
                    TagID3v2Header.IDv2TAGPresent = True
                    TagID3v2Header.VersionMajor = 3
                End If
                If Texte <> "" Then
                    Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte("", Texte)
                    ID3v2Modifie = True
                    NewFrame.ID = "TXXX"
                    NewFrame.Description = Description
                    NewFrame.Text = Texte
                    NewFrame.Size = Len(Texte)
                    TagID3v2Frames.Add(NewFrame)
                End If
            End Set
        End Property
        Public Property ID3v2_Commentaire(Optional ByVal Description As String = "") As String
            Get
                ID3v2_Commentaire = ""
                If ID3v2_Ok Then
                    TagID3v2Frames.ForEach(Sub(Element)
                                               If Element.ID = "COMM" And Element.Description = Description Then
                                                   ID3v2_Commentaire = Element.Text
                                                   Exit Sub
                                               End If
                                           End Sub)
                End If
            End Get
            Set(ByVal Texte As String)
                Dim NewFrame As New ID3v2Frame
                If ID3v2_Ok Then
                    Dim FrameRecherchee = From Element In TagID3v2Frames
                                            Where Element.ID = "COMM" And Element.Description = Description
                                            Select Element
                    If FrameRecherchee.Count > 0 Then
                        Dim OldFrame As ID3v2Frame = FrameRecherchee(0)
                        Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte(OldFrame.Text, Texte)
                        If Texte <> "" And Texte = OldFrame.Text Then Exit Property
                        ID3v2Modifie = True
                        TagID3v2Frames.Remove(OldFrame)
                        If Texte = "" Then Exit Property
                    End If
                Else
                    TagID3v2Header.IDv2TAGPresent = True
                    TagID3v2Header.VersionMajor = 3
                End If
                If Texte <> "" Then
                    Texte = TagID3.tagID3Object.FonctionUtilite.MiseAJourTexte("", Texte)
                    ID3v2Modifie = True
                    NewFrame.ID = "COMM"
                    NewFrame.Description = Description
                    NewFrame.Langue = "eng"
                    NewFrame.Text = Texte
                    TagID3v2Frames.Add(NewFrame)
                End If
            End Set
        End Property
        Public ReadOnly Property ID3v2_Image(ByVal Description As String) As ImageSource
            Get
                ID3v2_Image = Nothing
                If ID3v2_Ok Then
                    For Each Element In TagID3v2Frames
                        If Element.ID = "APIC" And Element.Description = Description Then
                            ID3v2_Image = FonctionUtilite.DownLoadImage(Element.BinaryData)
                            Exit Property
                        End If
                    Next Element
                End If
            End Get
        End Property
        Public ReadOnly Property ID3v2_Image() As ImageSource
            Get
                ID3v2_Image = Nothing
                If ID3v2_Ok Then
                    For Each Element In TagID3v2Frames
                        If Element.ID = "APIC" And Element.Description = "" Then
                            ID3v2_Image = FonctionUtilite.DownLoadImage(Element.BinaryData)
                            Exit Property
                        End If
                    Next Element
                End If
            End Get
        End Property
        Public Shared ReadOnly Property ID3v2_Image(ByVal Frames As List(Of ID3v2Frame)) As ImageSource
            Get
                ID3v2_Image = Nothing
                For Each Element In Frames
                    If Element.ID = "APIC" Then
                        ID3v2_Image = FonctionUtilite.DownLoadImage(Element.BinaryData)
                        Exit Property
                    End If
                Next Element
            End Get
        End Property
        Public ReadOnly Property ID3v2_ImageData(Optional ByVal Description As String = "") As Byte()
            Get
                If ID3v2_Ok Then
                    For Each Element In TagID3v2Frames
                        If Element.ID = "APIC" And Element.Description = Description Then
                            Return Element.BinaryData
                            Exit Property
                        End If
                    Next Element
                End If
            End Get
        End Property

        '***********************************************************************************************
        '-------------------------------METHODES PUBLICS DE LA CLASSE-----------------------------------
        '***********************************************************************************************        

        'PROCEDURES POUR LE CHARGEMENT D'IMAGES DANS L'OBJET
        Public Sub ID3v2_SetImage(ByVal FileNameImage As String, Optional ByVal Description As String = "", Optional ByVal ElementParent As UIElement = Nothing)
            Dim NewFrame As New ID3v2Frame
            If ID3v2_Ok Then
                Dim FrameRecherchee = From Element In TagID3v2Frames
                                        Where ((Element.Description = Description) And (Element.ID = "APIC"))
                                        Select Element
                If FrameRecherchee.Count > 0 Then
                    Dim OldFrame As ID3v2Frame = FrameRecherchee(0)
                    If Confirmation Then
                        Dim Action As String
                        If FileNameImage = "" Then Action = "supprimer" Else Action = "remplacer"
                        If Not wpfMsgBox.MsgBoxQuestion("MISE A JOUR IMAGE DU TITRE", _
                                                        "Voulez vous " & Action & " l'image " & IIf(Description = "", "par défaut", "du " & Description) & "?", ElementParent, Path.GetFileNameWithoutExtension(FileNameMp3)) Then Exit Sub
                        'If MsgBox("Voulez vous " & Action & " l'image existante pour la description : " _
                        '            & Chr(13) & Description & " ?", vbYesNo) <> vbYes Then Exit Sub
                    End If
                    ID3v2Modifie = True
                    TagID3v2Frames.Remove(OldFrame)
                    If FileNameImage = "" Then Exit Sub
                End If
            Else
                TagID3v2Header.IDv2TAGPresent = True
                TagID3v2Header.VersionMajor = 3
            End If
            If FileNameImage = "" Then Exit Sub
            ID3v2Modifie = True
            NewFrame.ID = "APIC"
            NewFrame.Text = "image/jpeg"
            NewFrame.Description = Description
            Dim Data As Byte() = FonctionUtilite.UploadImage(FileNameImage)
            If Data IsNot Nothing Then
                NewFrame.BinaryData = Data
                TagID3v2Frames.Add(NewFrame)
            End If
        End Sub
        Public Sub ID3v2_SetImage(ByVal TabBinaryData() As Byte, Optional ByVal Description As String = "", Optional ByVal ElementParent As UIElement = Nothing)
            Dim NewFrame As New ID3v2Frame
            If ID3v2_Ok Then
                Dim FrameRecherchee = From Element In TagID3v2Frames
                                        Where Element.ID = "APIC" And Element.Description = Description
                                        Select Element
                If FrameRecherchee.Count > 0 Then
                    Dim OldFrame As ID3v2Frame = FrameRecherchee(0)
                    If Confirmation Then
                        Dim Action As String
                        If TabBinaryData Is Nothing = "" Then Action = "supprimer" Else Action = "remplacer"
                        If Not wpfMsgBox.MsgBoxQuestion("MISE A JOUR IMAGE DU TITRE", _
                                                        "Voulez vous " & Action & " l'image " & IIf(Description = "", "par défaut", "du " & Description) & "?", ElementParent, Path.GetFileNameWithoutExtension(FileNameMp3)) Then Exit Sub
                        '   If MsgBox("Voulez vous " & Action & " l'image existante pour la description : " _
                        '               & Chr(13) & Description & " ?", vbYesNo) <> vbYes Then Exit Sub
                    End If
                    ID3v2Modifie = True
                    TagID3v2Frames.Remove(OldFrame)
                    If TabBinaryData Is Nothing Then Exit Sub
                End If
            Else
                TagID3v2Header.IDv2TAGPresent = True
                TagID3v2Header.VersionMajor = 3
            End If
            ID3v2Modifie = True
            NewFrame.ID = "APIC"
            NewFrame.Text = "image/jpeg"
            NewFrame.Description = Description
            Dim Data As Byte() = FonctionUtilite.UploadImage(TabBinaryData)
            If Data IsNot Nothing Then
                NewFrame.BinaryData = Data
                TagID3v2Frames.Add(NewFrame)
            End If
        End Sub

        'PROCEDURE D'ENREGISTREMENT DES INFORMATIONS DANS LE FICHIER
        '----------------------Traitement de l'enregistrement des fichiers MP3--------------------------
        Public Function SaveID3(Optional ByVal RemoveId3V1 As Boolean = False) As Boolean
            Dim NomFichier As String
            Dim NomFichierTemp As String
            Dim FichierMp3Temp As New fileBinary
            Dim TailleBuffer As Long
            Dim PosFinData As Long
            Dim PosStartData As Long
            Dim MemEcritureID3v1 As Boolean
            Dim Retour As Boolean
            If RemoveId3V1 Then If ID3v1Present Then ID3v1_Ok = False
            If (ID3v2Modifie Or ID3v1Modifie) And (FileNameMp3 IsNot Nothing) Then
                NomFichier = FileNameMp3
                NomFichierTemp = Path.ChangeExtension(NomFichier, ".~p3")
                'ECRITURE DES TAG ID3v2
                If Not ForcageLecture Then
                    If ID3v2Modifie Then
                        Try
                            FichierMp3Temp.CreateNewFile(NomFichierTemp)
                            ' FichierMp3Temp.NoException = False
                            ' FichierMp3.NoException = False
                            SyncLock LockOpen
                                If OpenFile(NomFichier) Then
                                    If TagID3v2Header.IDv2TAGPresent Then
                                        ID3v2_Texte("TENC") = "GBPlayer3"
                                        If WriteID3v2TAG(FichierMp3Temp) Then ID3v2Modifie = False
                                    Else
                                        ID3v2Modifie = False 'modif faite opur suppression des TAG v2
                                    End If
                                    'PROCEDURE D'ENREGISTREMENT SI PAS DEPASSEMENT TAILLE PRECEDENTE
                                    If FichierMp3Temp.PositionPointer = TagID3v2Header.PositionTrames Then
                                        TailleBuffer = FichierMp3Temp.PositionPointer
                                        Debug.Print(FichierMp3Temp.FileName & "SaveID3" & "Ecriture entete uniquement")
                                        'FichierMp3Temp.FlushBuffers()
                                        'FichierMp3Temp.CloseFile()
                                        'FichierMp3Temp.OpenFile(NomFichierTemp)
                                        Dim TabBuffer() As Byte
                                        FichierMp3Temp.ChangePointer(0, SeekOrigin.Begin)
                                        FichierMp3.ChangePointer(0, SeekOrigin.Begin)
                                        TabBuffer = FichierMp3Temp.ReadData(TailleBuffer)
                                        FichierMp3.WriteData(TabBuffer)
                                        FichierMp3Temp.CloseFile()
                                        FileSystem.DeleteFile(NomFichierTemp)
                                        If ID3v1Modifie Then ID3v1Modifie = Not (WriteID3v1TAG())
                                        CloseFile()
                                        Retour = True
                                    Else
                                        'PROCEDURE D'ENREGISTREMENT SI DIFFERENCE TAILLE PRECEDENTE
                                        If TagID3v2Header.PositionTrames > 0 Then PosStartData = TagID3v2Header.PositionTrames
                                        If ID3v1Modifie Then
                                            If ID3v1_Ok Then MemEcritureID3v1 = True Else ID3v1Modifie = False
                                            If ID3v1Present Then PosFinData = 128
                                        End If
                                        TailleBuffer = 2000000
                                        Debug.Print(FichierMp3Temp.FileName & "SaveID3" & "Reécriture de tout le fichier")
                                        Dim TabBuffer() As Byte
                                        FichierMp3.ChangePointer(PosStartData, SeekOrigin.Begin)
                                        Do While (TailleBuffer > 0)
                                            TabBuffer = FichierMp3.ReadData(TailleBuffer)
                                            FichierMp3Temp.WriteData(TabBuffer)
                                            If FichierMp3.FileSize - FichierMp3.PositionPointer - PosFinData < TailleBuffer Then
                                                TailleBuffer = FichierMp3.FileSize - FichierMp3.PositionPointer - PosFinData
                                            End If
                                        Loop
                                        'ECRITURE DES TAG ID3v1
                                        If MemEcritureID3v1 Then If WriteID3v1TAG(FichierMp3Temp) Then ID3v1Modifie = False
                                        'CLOTURE DE L'ENREGISTREMENT
                                        CloseFile()
                                        FichierMp3Temp.FlushBuffers()
                                        FichierMp3Temp.CloseFile()
                                        If Not ID3v1Modifie And Not ID3v2Modifie Then
                                            FileSystem.RenameFile(NomFichier, Path.GetFileNameWithoutExtension(NomFichier) & ".tem")
                                            FileSystem.RenameFile(NomFichierTemp, Path.GetFileName(NomFichier))
                                            FileSystem.DeleteFile(Path.ChangeExtension(NomFichier, ".Tem"))
                                            Thread.Sleep(300)
                                            Retour = True
                                        End If
                                    End If
                                Else
                                    If Not File.Exists(NomFichier) Then
                                        ID3v2Modifie = False
                                        ID3v1Modifie = False
                                        FichierMp3.NoException = True
                                        Return True
                                    End If
                                End If
                            End SyncLock
                        Catch ex As Exception
                            'If Confirmation Then
                            'MsgBox(ex.Message)
                            wpfMsgBox.MsgBoxInfo("Erreur TAGID3", ex.Message, Nothing)
                            FichierMp3.NoException = True
                            Return False
                        End Try
                        'OK UNIQUEMENT
                        'ENREGISTREMENT MODIFICATIONS DES TAG V1 UNIQUEMENT 
                    Else
                        If ID3v1Modifie Then
                            Try
                                ' FichierMp3.NoException = False
                                If OpenFile(NomFichier) Then
                                    ID3v1Modifie = Not (WriteID3v1TAG())
                                    CloseFile()
                                Else
                                    If Not File.Exists(NomFichier) Then
                                        ID3v2Modifie = False
                                        ID3v1Modifie = False
                                        FichierMp3.NoException = True
                                        Return True
                                    End If
                                End If
                            Catch ex As Exception
                                'MsgBox(ex.Message)
                                wpfMsgBox.MsgBoxInfo("Erreur TAGID3", ex.Message, Nothing)
                                FichierMp3.NoException = True
                                Return False
                            End Try
                        End If
                        If Not ID3v1Modifie Then Retour = True
                    End If
                    FichierMp3.NoException = True
                    'End SyncLock
                    If Retour Then
                        If ReadFile(NomFichier, True) Then InfosIsOk = True
                        Return True
                    Else
                        Return False
                    End If
                End If
            Else
                Return True
            End If
            Return True
        End Function

        'FUNCTION D'AIDE AU TRAITEMENT DES NOMS DES FICHIERS 'EXTRACTION ET CONSTRUCTION'
        '--------------Extraction des informations contenues dans le nom du fichier et du dossier-------
        Public Sub ExtractInfosTitre(ByVal ModeleExtractionFile As String, Optional ByVal ModeleExtractionRep As String = "")
            Dim Chaine As String
            Dim Result As String
            Dim ModeleEnCours As String
            Chaine = FonctionUtilite.NettoyageChaine(Path.GetFileNameWithoutExtension(FileNameMp3))
            If ModeleExtractionFile <> "" Then
                If DataConfig Is Nothing Then DataConfig = gbDev.TagID3Data.LoadConfig()
                ModeleEnCours = ModeleExtractionFile
                For c As Integer = 1 To 2
                    DataConfig.CHAINEEXTRATION_ID3v2.ForEach(Sub(i)
                                                                 Result = Trim(Extract(Chaine, ModeleEnCours, ExtraitChaine(i, "", "=")))
                                                                 If Result <> "" Then
                                                                     Select Case ExtraitChaine(i, ";", "")
                                                                         Case "N"
                                                                             Result = FonctionUtilite.NettoyageChaine(Result)
                                                                         Case "M"
                                                                             Result = StrConv(Result, VbStrConv.Uppercase)
                                                                     End Select
                                                                     ID3v2_Texte(ExtraitChaine(i, "=", ";")) = Result
                                                                 End If
                                                             End Sub)

                    If ModeleExtractionRep = "" Then Exit For
                    Chaine = Path.GetFileName(Path.GetDirectoryName(FileNameMp3))
                    ModeleEnCours = ModeleExtractionRep
                Next
                ID3v2_Texte("TENC") = "GBPlayer3"
            End If
        End Sub
        '--------------Normalise les informations TAG ID3v2 conformément au logiciel GBPlayer3-------
        Public Function NormaliseID3v2(ByVal ByValTestABlanc As Boolean) As Boolean
            Dim ModifFaite As Boolean
            Dim ListeIndex As New List(Of Integer)
            If DataConfig Is Nothing Then DataConfig = gbDev.TagID3Data.LoadConfig()
            TagID3v2Frames.ForEach(Sub(i As ID3v2Frame)
                                       Dim ValidationTag As Boolean
                                       DataConfig.TAGUTILISE_ID3v2.ForEach(Sub(c As String)
                                                                               If ExtraitChaine(c, "", ";") = i.ID Then
                                                                                   If i.Description IsNot Nothing Then
                                                                                       If ExtraitChaine(c, "[", "]") = i.Description Then
                                                                                           ValidationTag = True
                                                                                           Exit Sub
                                                                                       End If
                                                                                       Exit Sub
                                                                                   Else
                                                                                       ValidationTag = True
                                                                                       Return
                                                                                   End If
                                                                               End If
                                                                           End Sub)
                                       If ValidationTag Then
                                           Exit Sub
                                       Else
                                           ListeIndex.Add(TagID3v2Frames.IndexOf(i))
                                           If ByValTestABlanc Then Return
                                           ID3v2Modifie = True
                                           ModifFaite = True
                                       End If
                                   End Sub)
            If ListeIndex.Count > 0 Then
                If ByValTestABlanc Then Return True
                For v As Integer = ListeIndex.Count - 1 To 0 Step -1
                    TagID3v2Frames.RemoveAt(ListeIndex(v))
                Next
            End If
            Return ModifFaite
        End Function
        '--------------Nettoyage de la fin d'un fichier Mp3 pour supprimer le craquement de fin-------
        Public Function NettoyageFinMp3(ByVal TestABlanc As Boolean) As Integer
            Dim NbrPosASup As Integer
            Dim Retour As Boolean
            If InfosIsOk Then
                If OpenFile(FileNameMp3) Then
                    If ID3v1Present Then
                        FichierMp3.ChangePointer(-129, SeekOrigin.End)
                    Else
                        FichierMp3.ChangePointer(-1, SeekOrigin.End)
                    End If
                    Dim CarFin As Byte = FichierMp3.ReadByte()
                    FichierMp3.ChangePointer(-2, SeekOrigin.Current)
                    Do Until FichierMp3.PositionPointer = FichierMp3.FileSize
                        If (FichierMp3.ReadByte() = CarFin) Then
                            NbrPosASup = NbrPosASup + 1
                            FichierMp3.ChangePointer(-2, SeekOrigin.Current)
                        Else
                            Exit Do
                        End If
                    Loop
                    CloseFile()
                End If
                If TestABlanc And (NbrPosASup > 0) Then Return NbrPosASup
                If (Not TestABlanc) And (NbrPosASup > 0) Then
                    Dim FichierMp3Temp As New fileBinary
                    Dim NomFichier As String = FileNameMp3
                    Dim NomFichierTemp As String = Path.ChangeExtension(FileNameMp3, ".~p3")
                    Dim PosFinData As Long
                    Dim PosStartData As Long
                    Dim TailleBuffer As Long

                    FichierMp3Temp.CreateNewFile(NomFichierTemp)
                    If OpenFile(NomFichier) Then
                        If TagID3v2Header.IDv2TAGPresent Then
                            ID3v2_Texte("TENC") = "GBPlayer3"
                            ID3v2Modifie = True
                            If WriteID3v2TAG(FichierMp3Temp) Then ID3v2Modifie = False
                        End If

                        If ID3v2Present Then PosStartData = TagID3v2Header.PositionTrames
                        If ID3v1_Ok Then ID3v1Modifie = True Else ID3v1Modifie = False
                        If ID3v1Present Then PosFinData = 128
                        TailleBuffer = 2000000

                        Dim TabBuffer() As Byte
                        FichierMp3.ChangePointer(PosStartData, SeekOrigin.Begin)
                        Do While (TailleBuffer > 0)
                            TabBuffer = FichierMp3.ReadData(TailleBuffer)
                            FichierMp3Temp.WriteData(TabBuffer)
                            If FichierMp3.FileSize - FichierMp3.PositionPointer - PosFinData - NbrPosASup < TailleBuffer Then
                                TailleBuffer = FichierMp3.FileSize - FichierMp3.PositionPointer - PosFinData - NbrPosASup
                            End If
                        Loop
                        'ECRITURE DES TAG ID3v1
                        If ID3v1Modifie Then If WriteID3v1TAG(FichierMp3Temp) Then ID3v1Modifie = False
                        'CLOTURE DE L'ENREGISTREMENT
                        CloseFile()
                        FichierMp3Temp.FlushBuffers()
                        FichierMp3Temp.CloseFile()
                        If Not ID3v1Modifie And Not ID3v2Modifie Then
                            FileSystem.DeleteFile(NomFichier)
                            FileSystem.RenameFile(NomFichierTemp, Path.GetFileName(NomFichier))
                            Retour = True
                        End If
                    End If
                End If
                If Retour Then Return NbrPosASup
            End If
        End Function

        '----------------Renommage deu fichier----------------------------------------------------------
        Public Function RenameFile(ByVal NouveauNom As String, Optional ByVal ChaineDeMiseEnForme As String = "", Optional ByVal ABlanc As Boolean = False) As String
            Dim Sauvegarde As String
            Dim NonRename As Boolean
            Dim Chaine As String = ""
            If Me.InfosIsOk Then
                If NouveauNom = "" Then
                    If ChaineDeMiseEnForme <> "" Then
                        Chaine = ChaineDeMiseEnForme
                        If DataConfig Is Nothing Then DataConfig = gbDev.TagID3Data.LoadConfig()
                        DataConfig.CHAINEEXTRATION_ID3v2.ForEach(Sub(i)
                                                                     Dim MemChaine = Chaine
                                                                     Chaine = RemplaceChaine(Chaine, "%" & ExtraitChaine(i, "", "=") & "%", ID3v2_Texte(ExtraitChaine(i, "=", ";")))
                                                                     If ((MemChaine <> Chaine) And (ID3v2_Texte(ExtraitChaine(i, "=", ";")) = "")) Then NonRename = True
                                                                 End Sub)
                        Chaine = FonctionUtilite.NettoyageChaine(Chaine & ".mp3")
                        If NonRename Then Return Path.GetFileNameWithoutExtension(FileNameMp3)
                    Else
                        Return Path.GetFileNameWithoutExtension(FileNameMp3)
                    End If
                Else
                    If (NouveauNom = "") Or (UCase(GetFileExt(NouveauNom)) <> "MP3") Then Return ""
                    Chaine = NouveauNom
                End If
                If (Not ABlanc) Then
                    Sauvegarde = FileNameMp3
                    If (Chaine <> "") And (Sauvegarde <> GetFilePath(FileNameMp3) & "\" & Chaine) Then
                        Try
                            FileSystem.RenameFile(Sauvegarde, Chaine)
                            NomFichiermp3 = GetFilePath(FileNameMp3) & "\" & Chaine
                        Catch ex As Exception
                        End Try
                        Return NomFichiermp3
                    End If
                End If
            End If
            Return Chaine
        End Function

        '***********************************************************************************************
        '-------------------------------METHODES PRIVEES DE LA CLASSE---------------------------------
        '***********************************************************************************************
        '----------------------Fonction de nettoyage des infos du fichier MP3---------------------------
        Private Function CleanFrames(Optional ByVal ForcerFermeture As Boolean = False) As Boolean
            If (EnregistrementAuto And Not ForcerFermeture And Not ForcageLecture) Then Call SaveID3()
            TagID3v2Frames = Nothing
            ForcageLecture = False
            InfosIsOk = False
            Return True
        End Function
        '----------------------Fonction de lecture des infos du fichier MP3--------------------------------------
        Private Function ReadFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
            SyncLock LockOpen
                If OpenFile(Name, Forcage) Then
                    ID3v1Modifie = False
                    ID3v2Modifie = False
                    ID3v1Present = ReadID3v1TAG()
                    ID3v2Present = ReadID3v2TAG()
                    NomFichiermp3 = Name
                    CloseFile()
                    Return True
                End If
            End SyncLock
            Return False
        End Function
        '----------------------Fonction d'ouverture du fichier MP3--------------------------------------
        Private Function OpenFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
            Dim Access As FileAccess = FileAccess.ReadWrite
            Dim Share As FileShare = FileShare.ReadWrite
            Dim InfoFichier As FileInfo
            InfoFichier = New FileInfo(Name)
            If UCase(InfoFichier.Extension) = ".MP3" Then
                ForcageLecture = Forcage
                If (InfoFichier.Attributes And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then ForcageLecture = True
                If ForcageLecture Then
                    Access = FileAccess.Read
                    Share = FileShare.Read
                End If
                If FichierMp3.OpenFile(Name, Access, Share) Then
                    Return True
                Else
                    Debug.Print("Erreur ouverture de fichier tagID3")
                End If
            End If
            Return False
        End Function
        '----------------------Fonction de fermeture du fichier MP3-------------------------------------
        Private Function CloseFile() As Boolean
            If FichierMp3.FileIsOpen Then
                ' Debug.Print("tag closed : " & FileNameMp3)
                Call FichierMp3.FlushBuffers()
                If FichierMp3.CloseFile Then Return True
            End If
            Return False
        End Function

        'LECTURE ET ECRITURE DES TAG V2
        '--------------------------Fonction de lecture du TAGv1 dans le fichier mp3---------------------
        Private Function ReadID3v1TAG() As Boolean
            Dim StructureTAG As New IDTAG
            FichierMp3.ChangePointer(-128, SeekOrigin.End)
            StructureTAG.TAG = FichierMp3.ReadString(3)
            If (StructureTAG.TAG = "TAG") Then
                StructureTAG.Titre = FichierMp3.ReadString(30)
                StructureTAG.Artiste = FichierMp3.ReadString(30)
                StructureTAG.Album = FichierMp3.ReadString(30)
                StructureTAG.Annee = FichierMp3.ReadString(4)
                StructureTAG.Commentaire = FichierMp3.ReadString(30)
                StructureTAG.Genre = FichierMp3.ReadByte()
                TagID3v1 = StructureTAG
                Return True
            End If
            TagID3v1 = StructureTAG
            Return False
        End Function
        '--------------------------Fonction d'écriture du TAGv1 dans le fichier mp3---------------------
        Private Function WriteID3v1TAG(Optional ByVal NouveauMp3 As fileBinary = Nothing) As Boolean
            Dim LabelTAG As String
            Dim FichierEnCours As fileBinary
            If ID3v1Modifie Then
                If (NouveauMp3 Is Nothing) Then FichierEnCours = FichierMp3 Else FichierEnCours = NouveauMp3
                If ID3v1_Ok Then
                    If NouveauMp3 Is Nothing Then
                        FichierEnCours.ChangePointer(-128, SeekOrigin.End)
                        LabelTAG = FichierEnCours.ReadString(3)
                        If (LabelTAG = "TAG") Then
                            FichierEnCours.ChangePointer(-128, SeekOrigin.End)
                        Else
                            FichierEnCours.ChangePointer(0, SeekOrigin.End)
                        End If
                    Else
                        FichierEnCours.ChangePointer(0, SeekOrigin.End)
                    End If
                    FichierEnCours.WriteString(TagID3v1.TAG, 3)
                    FichierEnCours.WriteString(TagID3v1.Titre, 30)
                    FichierEnCours.WriteString(TagID3v1.Artiste, 30)
                    FichierEnCours.WriteString(TagID3v1.Album, 30)
                    FichierEnCours.WriteString(TagID3v1.Annee, 4)
                    FichierEnCours.WriteString(TagID3v1.Commentaire, 30)
                    FichierEnCours.WriteByte(TagID3v1.Genre)
                    ID3v1Modifie = False
                    Return True
                Else
                    If ID3v1Present Then
                        Return EffaceID3v1TAG()
                    Else
                        Return True
                    End If
                End If
            End If
            Return False
        End Function
        '--------------------------Fonction d'effacement du TAGv1 dans le fichier mp3-------------------
        Private Function EffaceID3v1TAG() As Boolean
            Dim NomFichier As String
            Dim NouveauNomFichier As String
            Dim NouveauMp3 As New fileBinary
            Dim TailleBuffer As Long
            Dim TabData() As Byte
            NomFichier = FichierMp3.FileName
            NouveauNomFichier = Path.ChangeExtension(NomFichier, "~p3")
            If NouveauMp3.CreateNewFile(NouveauNomFichier) And FichierMp3.FileIsOpen Then
                TailleBuffer = 100000
                FichierMp3.ChangePointer(0, SeekOrigin.Begin)
                Do While (TailleBuffer > 0)
                    If FichierMp3.FileSize - FichierMp3.PositionPointer - 128 < TailleBuffer Then
                        TailleBuffer = FichierMp3.FileSize - FichierMp3.PositionPointer - 128
                    End If
                    TabData = FichierMp3.ReadData(TailleBuffer)
                    NouveauMp3.WriteData(TabData)
                Loop
                Call CloseFile()
                NouveauMp3.CloseFile()
                Try
                    FileSystem.RenameFile(NomFichier, Path.GetFileName(Path.ChangeExtension(NomFichier, "~m3")))
                    FileSystem.RenameFile(NouveauNomFichier, Path.GetFileName(NomFichier))
                    FileSystem.DeleteFile(Path.ChangeExtension(NomFichier, "~m3"))
                    Debug.Print("Effacement ID3v1 : " & NomFichier)
                Catch ex As Exception
                    'MsgBox(ex.Message)
                    wpfMsgBox.MsgBoxInfo("Erreur TAGID3", ex.Message, Nothing)
                    Return False
                End Try
                TagID3v1 = Nothing
                ID3v1Present = False
                ID3v1Modifie = False
                Return True
            End If
            Return False
        End Function

        'LECTURE ET ECRITURE DES TAG V2
        '--------------------------Fonction de lecture de l'ensemble des TAGv2 dans le fichier mp3------
        Private Function ReadID3v2TAG() As Boolean
            Dim LectureByte As Byte
            Dim TailleRestante As Integer
            Dim SeuilTailleRestante As Integer
            Dim SizeBytes(0 To 3) As Byte
            Dim StructVide As New ID3v2Header
            Dim FramesVide As New List(Of ID3v2Frame)
            Dim TailleFrame As Integer
            'INITIALISATION
            FichierMp3.ChangePointer(0)
            TagID3v2Header = StructVide
            TagID3v2Frames = FramesVide
            'DETECTION DES INFORMATIONS DE TYPE ID3v2 EN TETE DE FICHIER
            If (FichierMp3.ReadString(3) = "ID3") Then
                'LECTURE PARTIE HEADER
                TagID3v2Header.IDv2TAGPresent = True
                TagID3v2Header.VersionMajor = FichierMp3.ReadByte()
                TagID3v2Header.VersionMinor = FichierMp3.ReadByte()
                LectureByte = FichierMp3.ReadByte()
                TagID3v2Header.TagUnsynchronised = CBool((LectureByte And 128) = 128)
                TagID3v2Header.ExtendedHeaderUsed = CBool((LectureByte And 64) = 64)
                TagID3v2Header.TagSize = FonctionUtilite.DecodeSynchSafe(FichierMp3.ReadData(4))
                TagID3v2Header.TotalTagSize = TagID3v2Header.TagSize + 10
                TailleRestante = TagID3v2Header.TagSize
                'LECTURE PARTIE EXTENDED HEADER SI EXISTANT
                If TagID3v2Header.ExtendedHeaderUsed Then
                    TagID3v2Header.ExtHeader_Size = FonctionUtilite.DecodeSize(FichierMp3.ReadData(4))
                    TagID3v2Header.ExtHeader_CRCPresent = CBool((FichierMp3.ReadByte() And &H80) = &H80)
                    LectureByte = FichierMp3.ReadByte()
                    TagID3v2Header.ExtHeader_PaddingSize = FonctionUtilite.DecodeSize(FichierMp3.ReadData(4)) 'Lecture Taille du padding
                    'LECTURE CRC32 SI EXISTANT
                    If TagID3v2Header.ExtHeader_CRCPresent Then
                        TagID3v2Header.CRC_TotalFrameCRC = FonctionUtilite.DecodeSize(FichierMp3.ReadData(4))
                    End If
                    TailleRestante = TagID3v2Header.TagSize - TagID3v2Header.ExtHeader_Size - 4
                End If
                '    If TagID3v2Header.TagUnsynchronised Then Exit Function 'LECTURE UNSYNCHRONISEE NON MAITRISEE
                '       ( tous les FF sont suivis par un 00 )
                'DEBUT DE LECTURE DES FRAMES
                If TagID3v2Header.VersionMajor = 2 Then SeuilTailleRestante = 6 Else SeuilTailleRestante = 10
                Do While TailleRestante >= SeuilTailleRestante + TagID3v2Header.ExtHeader_PaddingSize
                    TailleFrame = ReadID3v2Frame()
                    If TailleFrame <= 0 Then Exit Do
                    TailleRestante = TailleRestante - TailleFrame
                Loop
                Dim PositionDebutPadding As Long = FichierMp3.PositionPointer

                FichierMp3.ChangePointer(TagID3v2Header.ExtHeader_PaddingSize, SeekOrigin.Current)
                If ((FichierMp3.ReadByte() And 255) = 255) Then
                    If ((FichierMp3.ReadByte() And 224) = 224) Then
                        TagID3v2Header.PositionTrames = FichierMp3.PositionPointer - 2
                    End If
                End If

                If SearchPadding Then
                    If TagID3v2Header.PositionTrames = 0 Then
                        FichierMp3.ChangePointer(PositionDebutPadding, SeekOrigin.Begin)

                        'DETECTION DEBUT FICHIER MP3 AVEC LECTURE DE L'ENTETE DE LA PREMIERE TRAME
                        'Call FichierMp3.ChangePointer(TagID3v2Header.TagSize + SeuilTailleRestante, SeekOrigin.Begin)
                        Do Until FichierMp3.PositionPointer = FichierMp3.FileSize
                            If ((FichierMp3.ReadByte() And 255) = 255) Then
                                If ((FichierMp3.ReadByte() And 224) = 224) Then
                                    Call FichierMp3.ChangePointer(-1, SeekOrigin.Current)
                                    Exit Do
                                End If
                            End If
                        Loop
                        TagID3v2Header.PositionTrames = FichierMp3.PositionPointer - 1
                    End If
                    TagID3v2Header.ExtHeader_PaddingSize = TagID3v2Header.PositionTrames - PositionDebutPadding
                    ID3v2_EXTHEADER_PaddingSize = TagID3v2Header.ExtHeader_PaddingSize
                End If
                Return True
            End If
            Return False
        End Function
        '--------------------------Fonction de lecture d'une frame TAGv2 dans le fichier mp3---------
        Private Function ReadID3v2Frame() As Long
            Dim Lecture As Byte
            Dim SizeBytes(0 To 3) As Byte
            Dim Frame As New ID3v2Frame
            Dim Frameok As Boolean
            Select Case TagID3v2Header.VersionMajor
                Case 2
                    Frame.ID = FichierMp3.ReadString(3)
                    Frame.ID = ConvertTagID(TypeConversion_ID3v2.GBAU_ID3v2ID_CONV22_23, Frame.ID)
                    SizeBytes(1) = FichierMp3.ReadByte
                    SizeBytes(2) = FichierMp3.ReadByte
                    SizeBytes(3) = FichierMp3.ReadByte
                    Frame.Size = FonctionUtilite.DecodeSize(SizeBytes)
                    If Frame.Size > TagID3v2Header.TagSize Then Return -1
                Case 3, 4
                    Frame.ID = FichierMp3.ReadString(4)
                    SizeBytes = FichierMp3.ReadData(4)
                    Frame.Size = FonctionUtilite.DecodeSize(SizeBytes)
                    Lecture = FichierMp3.ReadByte()
                    Frame.DiscardIfTagAltered = CBool((Lecture And &H80) = &H80)
                    Frame.DiscardIfFileAltered = CBool((Lecture And &H40) = &H40)
                    Frame.LectureSeule = CBool((Lecture And &H20) = &H20)
                    Lecture = FichierMp3.ReadByte()
                    Frame.Compressed = CBool((Lecture And &H80) = &H80)
                    Frame.Encrypted = CBool((Lecture And &H40) = &H40)
                    Frame.GroupingIdentity = CBool((Lecture And &H20) = &H20)
                    If Frame.Size > TagID3v2Header.TagSize Then Return -1
            End Select
            Select Case Left(Frame.ID, 1)
                Case "T"
                    Frameok = ReadID3v2Frame_Text(Frame)
                Case "W"
                    Frameok = ReadID3v2Frame_Url(Frame)
                Case Else
                    Select Case Frame.ID
                        Case "COMM"
                            Frameok = ReadID3v2Frame_Commentaire(Frame)
                        Case "APIC"
                            Frameok = ReadID3v2Frame_Picture(Frame)
                        Case Else
                            If Frame.Size <= 0 Then
                                Return 0
                            Else
                                Frameok = ReadID3v2Frame_Autre(Frame)
                            End If

                    End Select
            End Select
            If Frameok Then TagID3v2Frames.Add(Frame)
            Return Frame.Size + IIf(TagID3v2Header.VersionMajor = 2, 6, 10)
        End Function
        '--------------------------Lecture d'une Frame de texte prédéfinie ou non-----------------------
        Private Function ReadID3v2Frame_Text(ByRef Frame As ID3v2Frame) As Boolean
            Dim Chaine As String
            Frame.TextEncoding = FichierMp3.ReadByte()
            Dim unicode As Boolean = (Frame.TextEncoding = 1)
            If Frame.Size - 1 = 0 Then Return False
            If UCase(Left(Frame.ID, 2)) = "TX" Then
                Chaine = FichierMp3.ReadString(Frame.Size - 1, True, unicode)
                Frame.Description = ExtraitChaine(Chaine, "", Chr(0))
                Frame.Description = Replace(Frame.Description, Chr(0), "")
                Frame.Text = ExtraitChaine(Chaine, Chr(0), "")
                Frame.Text = Replace(Frame.Text, Chr(0), "")
            Else
                Frame.Text = FichierMp3.ReadString(Frame.Size - 1, , unicode)
            End If
            Return True
        End Function
        '--------------------------Lecture d'une Frame URL prédéfinie ou non----------------------------
        Private Function ReadID3v2Frame_Url(ByRef Frame As ID3v2Frame) As Boolean
            Dim Chaine As String
            If UCase(Left(Frame.ID, 2)) = "WX" Then
                Frame.TextEncoding = FichierMp3.ReadByte()
                Dim unicode As Boolean = (Frame.TextEncoding = 1)
                If Frame.Size - 1 = 0 Then Return False
                Chaine = FichierMp3.ReadString(Frame.Size - 1, True, unicode)
                If Chaine.Length <= 1 Then Return False
                Frame.Description = ExtraitChaine(Chaine, "", Chr(0))
                Frame.Description = Replace(Frame.Description, Chr(0), "")
                Frame.Text = ExtraitChaine(Chaine, Chr(0), "")
                Frame.Text = Replace(Frame.Text, Chr(0), "")
            Else
                Frame.Text = FichierMp3.ReadString(Frame.Size)
            End If
            Return True
        End Function
        '--------------------------Lecture d'une Frame de commentaire-----------------------------------
        Private Function ReadID3v2Frame_Commentaire(ByRef Frame As ID3v2Frame) As Boolean
            Dim Chaine As String
            Frame.TextEncoding = FichierMp3.ReadByte()
            Dim unicode As Boolean = (Frame.TextEncoding = 1)
            Frame.Langue = FichierMp3.ReadString(3)
            If Frame.Size - 4 = 0 Then Return False
            Chaine = FichierMp3.ReadString(Frame.Size - 4, True, unicode)
            Frame.Description = ExtraitChaine(Chaine, "", Chr(0))
            Frame.Description = Replace(Frame.Description, Chr(0), "")
            Frame.Text = ExtraitChaine(Chaine, Chr(0), "")
            Frame.Text = Replace(Frame.Text, Chr(0), "")
            Return True
        End Function
        '--------------------------Lecture d'une frame image--------------------------------------------
        Private Function ReadID3v2Frame_Picture(ByRef Frame As ID3v2Frame) As Boolean
            Dim Chaine As String
            Dim SizeData As Long
            Frame.TextEncoding = FichierMp3.ReadByte
            SizeData = Frame.Size - 1
            If TagID3v2Header.VersionMajor = 2 Then
                Chaine = FichierMp3.ReadString(3)
                If Chaine = "PNG" Then Frame.Text = "image/png" Else Frame.Text = "image/jpeg"
                SizeData = SizeData - 3
            Else
                Frame.Text = FichierMp3.ReadString()
                SizeData = SizeData - Frame.Text.Length - 1
            End If
            Frame.TypeImage = FichierMp3.ReadByte()
            SizeData = SizeData - 1
            Frame.Description = FichierMp3.ReadString()
            SizeData = SizeData - Frame.Description.Length - 1
            If SizeData > 0 Then
                Frame.BinaryData = FichierMp3.ReadData(SizeData)
                Return True
            End If
            Return False
        End Function
        '--------------------------Lecture des frames non gérées----------------------------------------
        Private Function ReadID3v2Frame_Autre(ByRef Frame As ID3v2Frame) As Boolean
            If Frame.Size = 0 Then Return False
            Frame.BinaryData = FichierMp3.ReadData(Frame.Size)
            Return True
        End Function
        '--------------------------Fonction de conversion des TAG ID3v2 version 2.2 et 2.3--------------
        Private Function ConvertTagID(ByVal Sens As TypeConversion_ID3v2, ByVal IDTAG As String) As String
            If DataConfig Is Nothing Then DataConfig = gbDev.TagID3Data.LoadConfig()
            Select Case Sens
                Case TypeConversion_ID3v2.GBAU_ID3v2ID_CONV22_23
                    Dim FrameRecherchee = From Ligne In DataConfig.CONVERSION_ID3v22_ID3v23
                                         Where Split(Ligne, "=")(0) = IDTAG
                    If FrameRecherchee.Count > 0 Then
                        Return ExtraitChaine(FrameRecherchee(0), "=", "")
                    End If
                Case TypeConversion_ID3v2.GBAU_ID3v2ID_CONV23_22
                    Dim FrameRecherchee = From Ligne In DataConfig.CONVERSION_ID3v23_ID3v22
                                            Where Split(Ligne, "=")(0) = IDTAG
                    If FrameRecherchee.Count > 0 Then
                        Return ExtraitChaine(FrameRecherchee(0), "=", "")
                    End If
            End Select
            Return IDTAG
        End Function
        '--------------------------Ecriture de l'ensemble des TAGv2 dans le fichier mp3-----------------
        Private Function WriteID3v2TAG(ByVal NouveauMp3 As fileBinary) As Boolean
            Dim Ecriture As Byte
            Dim TailleId3v2 As Long
            Dim PosPointeur As Long
            NouveauMp3.ChangePointer(0)
            If ID3v2Modifie Then
                If ID3v2_Ok Then
                    'ECRITURE EN TETE DE FICHIER
                    NouveauMp3.WriteString("ID3")
                    'ECRITURE PARTIE HEADER
                    TagID3v2Header.VersionMajor = 3
                    NouveauMp3.WriteByte(TagID3v2Header.VersionMajor)
                    NouveauMp3.WriteByte(TagID3v2Header.VersionMinor)
                    '      If Me.ID3v2_HEADER_UnsynchronisationUsed Then Ecriture = &H80 Else Ecriture = 0
                    If ID3v2_HEADER_ExtendedHeaderUsed Then Ecriture = Ecriture + &H40
                    NouveauMp3.WriteByte(Ecriture)
                    NouveauMp3.WriteData(FonctionUtilite.EncodeSynchSafe(TagID3v2Header.TagSize))
                    'ECRITURE PARTIE EXTENDED HEADER SI EXISTANT
                    If TagID3v2Header.ExtendedHeaderUsed Then
                        If TagID3v2Header.ExtHeader_CRCPresent Then TagID3v2Header.ExtHeader_Size = 10 Else TagID3v2Header.ExtHeader_Size = 6
                        NouveauMp3.WriteData(FonctionUtilite.EncodeSize(TagID3v2Header.ExtHeader_Size))
                        If TagID3v2Header.ExtHeader_CRCPresent Then Ecriture = &H80 Else Ecriture = 0
                        NouveauMp3.WriteByte(Ecriture)
                        NouveauMp3.WriteByte(0)
                        NouveauMp3.WriteData(FonctionUtilite.EncodeSize(0))
                        TailleId3v2 = 10
                        'ECRITURE CRC32 SI EXISTANT
                        If TagID3v2Header.ExtHeader_CRCPresent Then
                            NouveauMp3.WriteData(FonctionUtilite.EncodeSize(TagID3v2Header.CRC_TotalFrameCRC))
                            TailleId3v2 = TailleId3v2 + 4
                        End If
                    End If
                    'DEBUT DE L'ECRITURE DES FRAMES
                    TagID3v2Frames.ForEach(Sub(Frame) TailleId3v2 = TailleId3v2 + WriteID3v2Frame(Frame, NouveauMp3))
                    'ECRITURE DU PADDING
                    If lPaddingSizeWanted = -1 And TagID3v2Header.ExtendedHeaderUsed Then
                        'Recalcule de la taille du padding
                        If (TailleId3v2 < TagID3v2Header.TagSize) And (TagID3v2Header.TagSize - TailleId3v2 <= 1000) Then
                            lPaddingSizeWanted = TagID3v2Header.TagSize - TailleId3v2
                        Else
                            lPaddingSizeWanted = 200
                        End If
                    End If
                    If lPaddingSizeWanted > 0 Then
                        TagID3v2Header.ExtHeader_PaddingSize = lPaddingSizeWanted
                        Dim Padding(TagID3v2Header.ExtHeader_PaddingSize - 1) As Byte
                        NouveauMp3.WriteData(Padding)
                        TailleId3v2 = TailleId3v2 + TagID3v2Header.ExtHeader_PaddingSize
                    Else
                        TagID3v2Header.ExtHeader_PaddingSize = 0
                    End If
                    PosPointeur = NouveauMp3.PositionPointer
                    'Ecriture de la taille du padding
                    If TagID3v2Header.ExtHeader_PaddingSize <> 0 Then
                        NouveauMp3.ChangePointer(16, SeekOrigin.Begin)
                        NouveauMp3.WriteData(FonctionUtilite.EncodeSize(TagID3v2Header.ExtHeader_PaddingSize))
                    End If
                    'ECRITURE DE LA TAILLE DANS L'ENTETE
                    TagID3v2Header.TagSize = TailleId3v2
                    TagID3v2Header.TotalTagSize = PosPointeur
                    NouveauMp3.ChangePointer(6, SeekOrigin.Begin)
                    NouveauMp3.WriteData(FonctionUtilite.EncodeSynchSafe(TagID3v2Header.TagSize))
                    NouveauMp3.ChangePointer(PosPointeur, SeekOrigin.Begin)
                    Return True
                End If
            End If
            Return False
        End Function
        '--------------------------Ecriture d'une frame TAGv2 dans le fichier mp3-----------------------
        Private Function WriteID3v2Frame(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            Dim Ecriture As Byte
            Dim SizeBytes() As Byte
            Dim PosFrameSize As Long
            Dim PosFrameEnd As Long
            Dim FrameHeaderSize As Long
            Dim FrameSize As Long
            Select Case TagID3v2Header.VersionMajor
                Case 2
                    NouveauMp3.WriteString(ConvertTagID(TypeConversion_ID3v2.GBAU_ID3v2ID_CONV23_22, Frame.ID))
                    PosFrameSize = NouveauMp3.PositionPointer
                    SizeBytes = FonctionUtilite.EncodeSize(Frame.Size)
                    NouveauMp3.WriteByte(SizeBytes(1))
                    NouveauMp3.WriteByte(SizeBytes(2))
                    NouveauMp3.WriteByte(SizeBytes(3))
                    FrameHeaderSize = 6
                Case 3, 4
                    NouveauMp3.WriteString(Frame.ID)
                    PosFrameSize = NouveauMp3.PositionPointer
                    NouveauMp3.WriteData(FonctionUtilite.EncodeSize(Frame.Size))
                    If Frame.DiscardIfTagAltered Then Ecriture = &H80 Else Ecriture = 0
                    If Frame.DiscardIfFileAltered Then Ecriture = Ecriture + &H40
                    If Frame.LectureSeule Then Ecriture = Ecriture + &H20
                    NouveauMp3.WriteByte(Ecriture)
                    If Frame.Compressed Then Ecriture = &H80 Else Ecriture = 0
                    If Frame.Encrypted Then Ecriture = Ecriture + &H40
                    If Frame.GroupingIdentity Then Ecriture = Ecriture + &H20
                    NouveauMp3.WriteByte(Ecriture)
                    FrameHeaderSize = 10
            End Select
            Select Case Left(Frame.ID, 1)
                Case "T"
                    FrameSize = WriteID3v2Frame_Text(Frame, NouveauMp3)
                Case "W"
                    FrameSize = WriteID3v2Frame_Url(Frame, NouveauMp3)
                Case Else
                    Select Case Frame.ID
                        Case "COMM"
                            FrameSize = WriteID3v2Frame_Commentaire(Frame, NouveauMp3)
                        Case "APIC"
                            FrameSize = WriteID3v2Frame_Picture(Frame, NouveauMp3)
                        Case Else
                            FrameSize = WriteID3v2Frame_Autre(Frame, NouveauMp3)
                    End Select
            End Select
            PosFrameEnd = NouveauMp3.PositionPointer
            NouveauMp3.ChangePointer(PosFrameSize, SeekOrigin.Begin)
            Select Case TagID3v2Header.VersionMajor
                Case 2
                    SizeBytes = FonctionUtilite.EncodeSize(FrameSize)
                    NouveauMp3.WriteByte(SizeBytes(1))
                    NouveauMp3.WriteByte(SizeBytes(2))
                    NouveauMp3.WriteByte(SizeBytes(3))
                Case 3, 4
                    NouveauMp3.WriteData(FonctionUtilite.EncodeSize(FrameSize))
            End Select
            NouveauMp3.ChangePointer(PosFrameEnd, SeekOrigin.Begin)
            Return FrameSize + FrameHeaderSize
        End Function
        '--------------------------Ecriture d'une Frame de texte prédéfinie ou non----------------------
        Private Function WriteID3v2Frame_Text(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            Dim Chaine As String
            Dim LenghtChaine As Integer
            Dim unicode As Boolean = (Frame.TextEncoding = 1)
            'If unicode Then
            'FORCAGE ECRITURE EN UNICODE POUR LES TEXTES
            NouveauMp3.WriteByte(1)
            ' Else
            'NouveauMp3.WriteByte(0)
            'End If
            If StrConv(Left(Frame.ID, 2), VbStrConv.Uppercase) = "TX" Then
                Chaine = Frame.Description & Chr(0) & Frame.Text
                LenghtChaine = NouveauMp3.WriteStringU(Chaine)
            Else
                Chaine = Frame.Text
                LenghtChaine = NouveauMp3.WriteStringU(Frame.Text)
            End If
            Return LenghtChaine + 1
        End Function
        '--------------------------Ecriture d'une Frame URL---------------------------------------------
        Private Function WriteID3v2Frame_Url(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            Dim Chaine As String
            If StrConv(Left(Frame.ID, 2), VbStrConv.Uppercase) = "WX" Then
                NouveauMp3.WriteByte(0)
                Chaine = Frame.Description & Chr(0) & Frame.Text
                NouveauMp3.WriteString(Chaine)
                Return Chaine.Length + 1
            Else
                Chaine = Frame.Text
                NouveauMp3.WriteString(Frame.Text)
                Return Chaine.Length
            End If
        End Function
        '--------------------------Ecriture d'une Frame Commentaire-------------------------------------
        Private Function WriteID3v2Frame_Commentaire(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            Dim Chaine As String
            NouveauMp3.WriteByte(0)
            NouveauMp3.WriteString(Frame.Langue, 3)
            Chaine = Frame.Description & Chr(0) & Frame.Text
            NouveauMp3.WriteString(Chaine)
            Return Chaine.Length + 4
        End Function
        '--------------------------Ecriture d'une Frame image666666666----------------------------------
        Private Function WriteID3v2Frame_Picture(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            Dim Chaine As String
            Dim FrameSize As Long
            NouveauMp3.WriteByte(0)
            If TagID3v2Header.VersionMajor = 2 Then
                If Frame.Text = "image/png" Then Chaine = "PNG" Else Chaine = "JPG"
                NouveauMp3.WriteString(Chaine)
                FrameSize = 4
            Else
                Chaine = Frame.Text & Chr(0)
                NouveauMp3.WriteString(Chaine)
                FrameSize = Chaine.Length + 1
            End If
            NouveauMp3.WriteByte(Frame.TypeImage)
            FrameSize = FrameSize + 1
            'If Frame.Description <> "" Then
            Chaine = Frame.Description & Chr(0)
            NouveauMp3.WriteString(Chaine)
            'End If
            FrameSize = FrameSize + Chaine.Length
            If Frame.BinaryData.Length <> 0 Then
                NouveauMp3.WriteData(Frame.BinaryData)
                FrameSize = FrameSize + Frame.BinaryData.Length
            End If
            Return FrameSize
        End Function
        '--------------------------Ecriture d'une Frame autre-------------------------------------------
        Private Function WriteID3v2Frame_Autre(ByVal Frame As ID3v2Frame, ByVal NouveauMp3 As fileBinary) As Long
            If Frame.BinaryData.Length <> 0 Then
                NouveauMp3.WriteData(Frame.BinaryData)
                Return Frame.BinaryData.Length
            End If
            Return 0
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' Pour détecter les appels redondants
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    CleanFrames()
                End If
            End If
            Me.disposedValue = True
        End Sub
        ' Ce code a été ajouté par Visual Basic pour permettre l'implémentation correcte du modèle pouvant être supprimé.
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
        '***********************************************************************************************
        '----------------------CLASSE PRIVEE REGROUPANT LES FONCTIONS D'UTILITE DE LA CLASSE------------
        '***********************************************************************************************
        Class FonctionUtilite
            '-----------Fonction de transfert de l'image depuis le TAG------------------------------------------
            Friend Shared Function DownLoadImage(ByVal BinaryData() As Byte) As ImageSource
                Dim Image As New BitmapImage
                If BinaryData IsNot Nothing Then
                    Try
                        Image.BeginInit()
                        Image.StreamSource = New MemoryStream(BinaryData)
                        Image.EndInit()
                        Return Image
                    Catch ex As Exception
                    End Try
                End If
                Return Nothing
            End Function
            Friend Shared Function GetBitmapData(ByVal Streamdata As BitmapImage) As Byte()
                Dim pf As PixelFormat = Streamdata.Format
                Dim width As Integer = Streamdata.PixelWidth
                Dim height As Integer = Streamdata.PixelHeight
                Dim Stride As Integer = CType((width * pf.BitsPerPixel + 7) / 8, Integer)
                Dim buffer(height * Stride - 1) As Byte
                Try
                    Streamdata.CopyPixels(buffer, Stride, 0)
                Catch ex As Exception
                End Try
                Return buffer
            End Function
            Friend Shared Function UploadImage(ByVal FileNameImage As String) As Byte()

                Try
                    Dim UriFileName As New Uri(FileNameImage)
                    Dim Image = New BitmapImage()
                    If UriFileName.Scheme = Uri.UriSchemeFile Then
                        Try
                            Image.BeginInit()
                            Image.UriSource = UriFileName
                            Image.DecodePixelHeight = 600
                            Image.DecodePixelWidth = 600
                            Image.EndInit()
                        Catch ex As Exception
                            Return Nothing
                        End Try
                        'End If
                    ElseIf UriFileName.Scheme = Uri.UriSchemeHttp Then
                        Dim WebClient As New System.Net.WebClient
                        WebClient.Headers.Add("user-agent", "GBPlayer3")
                        Dim Data As Byte() = WebClient.DownloadData(UriFileName)
                        Try
                            Image.BeginInit()
                            Image.StreamSource = New MemoryStream(Data)
                            Image.DecodePixelHeight = 600
                            Image.DecodePixelWidth = 600
                            Image.EndInit()
                        Catch ex As Exception
                            wpfMsgBox.MsgBoxInfo("Erreur de chargement", Text.Encoding.ASCII.GetString(Data), Nothing)
                            Return Nothing
                        Finally
                            WebClient.Dispose()
                            WebClient = Nothing
                        End Try
                    Else
                        Return Nothing
                    End If
                    Try
                        Dim Tempstream As New MemoryStream()
                        Dim Encodeur As New JpegBitmapEncoder()
                        'Encodeur.FlipHorizontal = False
                        'Encodeur.FlipVertical = False
                        'Encodeur.Rotation = Rotation.Rotate0
                        Encodeur.QualityLevel = 90
                        Encodeur.Frames.Add(BitmapFrame.Create(Image))
                        Encodeur.Save(Tempstream)
                        Dim Buffer(Tempstream.Length - 1) As Byte
                        Tempstream.Seek(0, SeekOrigin.Begin)
                        Tempstream.Read(Buffer, 0, Tempstream.Length)
                        Return Buffer
                    Catch ex As Exception
                    End Try
                Catch ex As Exception
                    'wpfMsgBox.MsgBoxInfo("Erreur TAGID3", ex.Message, Nothing)
                    'MsgBox(ex.Message)
                End Try
                Return Nothing
            End Function
            Friend Shared Function UploadImage(ByVal TabBinayData() As Byte) As Byte()
                Dim Image = New BitmapImage()
                Try
                    Dim Tempstream As New MemoryStream()
                    Dim Encodeur As New JpegBitmapEncoder()
                    Image.BeginInit()
                    Image.StreamSource = New MemoryStream(TabBinayData)
                    Image.DecodePixelHeight = 600
                    Image.DecodePixelWidth = 600
                    Image.EndInit()
                    'Encodeur.FlipHorizontal = False
                    'Encodeur.FlipVertical = False
                    'Encodeur.Rotation = Rotation.Rotate0
                    Encodeur.QualityLevel = 90
                    Encodeur.Frames.Add(BitmapFrame.Create(Image))
                    Encodeur.Save(Tempstream)
                    Dim Buffer(Tempstream.Length - 1) As Byte
                    Tempstream.Seek(0, SeekOrigin.Begin)
                    Tempstream.Read(Buffer, 0, Tempstream.Length)
                    Return Buffer
                Catch ex As Exception
                End Try
                Return Nothing
            End Function
            Friend Shared Function UploadImage(ByVal NewUri As Uri) As Byte() 'A REVOIR UTILITEE
                Try
                    Dim Image = New BitmapImage(NewUri)
                    '  Try
                    ' Image.BeginInit()
                    ' Image.UriSource = NewUri
                    ' Image.EndInit()
                    'Catch ex As Exception
                    '    Return Nothing
                    'End Try
                    Try
                        Dim Tempstream As New MemoryStream
                        Dim Encodeur As New JpegBitmapEncoder()
                        'Encodeur.FlipHorizontal = False
                        'Encodeur.FlipVertical = False
                        'Encodeur.Rotation = Rotation.Rotate0
                        Encodeur.QualityLevel = 90
                        Encodeur.Frames.Add(BitmapFrame.Create(Image))
                        Encodeur.Save(Tempstream)
                        Dim Buffer(Tempstream.Length - 1) As Byte
                        Tempstream.Seek(0, SeekOrigin.Begin)
                        Tempstream.Read(Buffer, 0, Tempstream.Length)
                        Return Buffer
                    Catch ex As Exception
                    End Try
                Catch ex As Exception
                End Try
                Return Nothing
            End Function
            Friend Shared Function UploadImage(ByVal Image As ImageSource) As Byte()
                Try
                    Dim Tempstream As New MemoryStream()
                    Dim Encodeur As New JpegBitmapEncoder()
                    Encodeur.QualityLevel = 90
                    Encodeur.Frames.Add(BitmapFrame.Create(Image))
                    Encodeur.Save(Tempstream)
                    Dim Buffer(Tempstream.Length - 1) As Byte
                    Tempstream.Seek(0, SeekOrigin.Begin)
                    Tempstream.Read(Buffer, 0, Tempstream.Length)
                    Return Buffer
                Catch ex As Exception
                End Try
                Return Nothing
            End Function
            Friend Shared Function ConvertJpegdataToDibstream(ByVal BinaryData As Byte()) As Stream
                Try

                    Dim Tempstream As New MemoryStream()
                    Tempstream.Write(BinaryData, 0, BinaryData.Length)
                    Tempstream.Position = 0
                    Dim PremierCaractere = Tempstream.ReadByte()
                    Tempstream.Position = 0
                    Dim Decoder As BitmapDecoder
                    If PremierCaractere = 255 Then
                        Decoder = New JpegBitmapDecoder(Tempstream,
                                                        BitmapCreateOptions.PreservePixelFormat Or
                                                        BitmapCreateOptions.IgnoreColorProfile,
                                                        BitmapCacheOption.Default)
                    Else
                        Decoder = New BmpBitmapDecoder(Tempstream,
                                                        BitmapCreateOptions.PreservePixelFormat Or
                                                        BitmapCreateOptions.IgnoreColorProfile,
                                                        BitmapCacheOption.Default)
                    End If
                    Dim Image As BitmapFrame = Decoder.Frames(0)
                    Dim encoder As BmpBitmapEncoder = New BmpBitmapEncoder()
                    encoder.Frames.Add(Image)
                    Dim BitmapStream As New MemoryStream()
                    encoder.Save(BitmapStream)
                    Dim DibStream As New MemoryStream
                    BitmapStream.Seek(14, SeekOrigin.Begin)
                    BitmapStream.CopyTo(DibStream, BitmapStream.Length - 14)
                    Return DibStream
                Catch ex As Exception
                    Debug.Print(ex.Message)
                End Try
            End Function
            Friend Shared Function ConvertDibstreamToJpegdata(ByVal dibStream As Stream) As Byte()
                dibStream.Seek(0, SeekOrigin.Begin)
                Dim reader As BinaryReader = New BinaryReader(dibStream)
                Dim headersize As Integer = reader.ReadInt32
                Dim pixelsize As Integer = dibStream.Length - headersize
                Dim filesize As Integer = 14 + headersize + pixelsize
                Dim bmp As MemoryStream = New MemoryStream(filesize)
                Dim writer As BinaryWriter = New BinaryWriter(bmp)
                'ECRITURE ENTETE BITMAP
                writer.Write(CType(Asc("B"), Byte))
                writer.Write(CType(Asc("M"), Byte))
                writer.Write(filesize)
                writer.Write(0)
                writer.Write(14 + headersize)
                'COPY LE DIB
                dibStream.Position = 0
                Dim data(dibStream.Length - 1) As Byte
                dibStream.Read(data, 0, dibStream.Length)
                writer.Write(data, 0, data.Length)
                bmp.Position = 0
                Dim Buffer(bmp.Length - 1) As Byte
                bmp.Read(Buffer, 0, bmp.Length)
                Return UploadImage(Buffer)
            End Function
            '-----------Fonction de redimessionnement d'une image-----------------------------------------------
            Friend Shared Function CreateResizedImage(ByVal source As ImageSource, ByVal width As Integer, ByVal height As Integer) As ImageSource
                Dim rect As New Rect(0, 0, width, height)
                Dim drawingVisual As New DrawingVisual()
                Using drawingContext As DrawingContext = drawingVisual.RenderOpen()
                    drawingContext.DrawImage(source, rect)
                End Using
                Dim resizedImage As New RenderTargetBitmap(CInt(rect.Width), CInt(rect.Height), 96, 96, PixelFormats.[Default])
                resizedImage.Render(drawingVisual)
                Return resizedImage
            End Function
            '-----------Fonction d'enregistrement sur disque de l'image d'un fichier---------------------------
            Friend Shared Function SaveImage(ByVal BinaryData() As Byte, ByVal FileNameImage As String) As Boolean
                Dim Tempstream As New FileStream(FileNameImage, FileMode.Create)
                Tempstream.Write(BinaryData, 0, BinaryData.Length)
                Tempstream.Close()
                Return True
            End Function
            Friend Shared Function SaveImage(ByVal Image As BitmapImage, ByVal FileNameImage As String) As Boolean
                Try
                    Dim Tempstream As New MemoryStream()
                    Dim Encodeur As New JpegBitmapEncoder()
                    Encodeur.QualityLevel = 90
                    Encodeur.Frames.Add(BitmapFrame.Create(Image))
                    Encodeur.Save(Tempstream)
                    Dim Buffer(Tempstream.Length - 1) As Byte
                    Tempstream.Seek(0, SeekOrigin.Begin)
                    Tempstream.Read(Buffer, 0, Tempstream.Length)
                    TagID3.tagID3Object.FonctionUtilite.SaveImage(Buffer, FileNameImage)
                Catch ex As Exception
                End Try
            End Function

            '-----------Fonction utilisée pour la mise en forme des TAG des fichiers mp3----------------------
            Friend Shared Function NettoyageChaine(ByVal TitreExistant As String) As String
                Dim Chaine As String = TitreExistant
                If Chaine <> "" Then
                    Chaine = Replace(Chaine, Chr(0), "")
                    Chaine = Replace(Chaine, "+", " ")
                    Chaine = Replace(Chaine, "%20", " ")
                    Chaine = Replace(Chaine, "%26", "&")
                    Chaine = Replace(Chaine, "%27", "'")
                    Chaine = Replace(Chaine, "%28", "[")
                    Chaine = Replace(Chaine, "%29", "]")
                    Chaine = Replace(Chaine, """", "''")
                    Chaine = Replace(Chaine, "/", "_")
                    Chaine = Replace(Chaine, "\", "_")
                    Chaine = Replace(Chaine, "*", "_")
                    Chaine = Replace(Chaine, "?", "_")
                    Chaine = Replace(Chaine, ":", "_")
                    Chaine = Replace(Chaine, "<", "_")
                    Chaine = Replace(Chaine, ">", "_")
                    Chaine = Replace(Chaine, "|", "_")
                    '     Chaine = Replace(Chaine, "_", " ")
                    Chaine = StrConv(Chaine, vbProperCase)
                    Return Trim(Chaine)
                End If
                Return ""
            End Function
            '-----------Fonction de la mise en forme avec majuscule au debut de chaque mot--------------------
            Friend Shared Function MiseEnFormeChaine(ByVal TitreExistant As String) As String
                Dim Chaine As String = TitreExistant
                If Chaine <> "" Then
                    If Right(Chaine, 1) = ")" Or Right(Chaine, 1) = "]" Then
                        Chaine = Replace(Chaine, "[", "(")
                        Chaine = Replace(Chaine, "]", ")")
                        Dim tabChaine = Split(Chaine, "(")
                        Dim LongueurRight As Integer = tabChaine.Last.Length + 1
                        Dim NouvelleChaine = Right(Chaine, LongueurRight)
                        NouvelleChaine = Replace(NouvelleChaine, ")", "]")
                        NouvelleChaine = Replace(NouvelleChaine, "(", "[")
                        Chaine = Left(Chaine, Chaine.Length - LongueurRight) & NouvelleChaine
                        ' Chaine = StrConv(Chaine, vbProperCase)
                        'Return Trim(Chaine)
                    End If
                    '   Chaine = Replace(Chaine, ")", "]")
                    '   Chaine = Replace(Chaine, "(", "[")
                    Chaine = StrConv(Chaine, vbProperCase)
                    Return Trim(Chaine)
                End If
                Return ""
            End Function
            '-----------Fonction de la mise à jour de texte---------------------------------------------------
            Friend Shared Function MiseAJourTexte(ByVal TexteOriginal As String, ByVal NouveauTexte As String) As String
                If (Right(NouveauTexte, 1) = "/") And (Left(NouveauTexte, 1) = "*") Then
                    Return RemplaceChaine(NouveauTexte, "*", ExtraitChaine(TexteOriginal, "", "/", , True))
                End If
                If (InStr(NouveauTexte, "*", CompareMethod.Text) = 0) Then
                    Return NouveauTexte
                Else
                    Return RemplaceChaine(NouveauTexte, "*", TexteOriginal)
                End If
            End Function
            '-----------Fonction de décodage des tailles calculées sur 3 bytes utilisées dans ID3v2-----------
            Friend Shared Function DecodeSynchSafe(ByVal bytBytes() As Byte) As Integer
                Return ((CInt(bytBytes(0)) << 21) + (CInt(bytBytes(1)) << 14) + (CInt(bytBytes(2)) << 7) + (CInt(bytBytes(3))))
            End Function
            '-----------Fonction d'encodage des tailles calculées sur 3 bytes utilisées dans ID3v2------------
            Public Shared Function EncodeSynchSafe(ByVal Size As Long) As Byte()
                Dim TabSize(3) As Byte
                TabSize(0) = CByte((Size >> 21) And &H7F)
                TabSize(1) = CByte((Size >> 14) And &H7F)
                TabSize(2) = CByte((Size >> 7) And &H7F)
                TabSize(3) = CByte(Size And &H7F)
                Return TabSize
            End Function
            '-----------Fonction de décodage des tailles calculées sur 4 bytes utilisées dans ID3v2-----------
            Friend Shared Function DecodeSize(ByVal bytBytes() As Byte) As Integer
                If bytBytes Is Nothing Then Return 0
                Return (CInt(bytBytes(0)) << 24) + (CInt(bytBytes(1)) << 16) + (CInt(bytBytes(2)) << 8) + CInt(bytBytes(3))
            End Function
            '-----------Fonction d'encodage des tailles calculées sur 4 bytes utilisées dans ID3v2------------
            Public Shared Function EncodeSize(ByVal Size As Long) As Byte()
                Dim TabSize(3) As Byte
                TabSize(0) = CByte((Size >> 24) And &HFF)
                TabSize(1) = CByte((Size >> 16) And &HFF)
                TabSize(2) = CByte((Size >> 8) And &HFF)
                TabSize(3) = CByte(Size And &HFF)
                Return TabSize
            End Function
        End Class
    End Class
End Namespace
