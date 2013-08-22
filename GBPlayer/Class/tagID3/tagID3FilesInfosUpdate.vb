Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 09/12/11
'DESCRIPTION :Classes utilités pour la gestion mise a jour des fichiers en tache de fond dans 
'               un autre thread
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO
Imports System.Threading


'***********************************************************************************************
'----------------------------CLASSE PERMETTANT L'ENREGISTREMENT DES INFOS EN TACHE DE FOND------
'***********************************************************************************************
Public Class tagID3FilesInfosUpdate
    Private Shared Property TopFenetreParente As Integer
    Private Shared Property LeftFenetreParente As Integer
    Private Shared Property MaJTerminee As Boolean
    Private Shared _ListeTacheAExecuter As New List(Of tagID3FilesInfos)
    Private Shared MaxFichiers As Integer
    Private Shared NewThread As Thread
    Private Shared Fenetre As MainWindow
    Private Shared FenetreInfo As WindowUpdateProgress
    Shared Sub New()
        Fenetre = CType(Application.Current.MainWindow, MainWindow)
        FenetreInfo = CType(Application.Current.MainWindow, MainWindow).ProcessMiseAJour
    End Sub

    Private Shared NumProcess As Long
    Public Shared Sub UpdateInfosFichiers(ByVal Infos As tagID3FilesInfosDO)
        If MaJTerminee Then MaxFichiers = 0
        MaJTerminee = False
        _ListeTacheAExecuter.Add(New tagID3FilesInfos(Infos))
        UpdateWindows("#INIT#" & Infos.ListeFichiers.Count)
        MaxFichiers += Infos.ListeFichiers.Count
        If NewThread Is Nothing Then
            NewThread = New Thread(AddressOf WriteFilesModif)
            NewThread.SetApartmentState(ApartmentState.STA)
            NewThread.Start()
        ElseIf Not NewThread.IsAlive Then
            NewThread = New Thread(AddressOf WriteFilesModif)
            NewThread.SetApartmentState(ApartmentState.STA)
            NewThread.Start()
        End If
    End Sub
    '.... A personnaliser pour chaque élément à enregistrer.........
    Private Delegate Sub UpdateWindowsDelegate(ByVal NomFichier As String)
    Private Shared Sub UpdateWindows(ByVal NomFichier As String)
        If InStr(NomFichier, "#INIT#") Then
            NumProcess = FenetreInfo.AddNewProcess(CInt(ExtraitChaine(NomFichier, "#INIT#", "", 6)), NumProcess)
        ElseIf NomFichier = "#END#" Then
            FenetreInfo.StopProcess(NumProcess)
            NumProcess = 0
        Else
            FenetreInfo.UpdateWindows(NomFichier, NumProcess)
        End If
    End Sub
    Private Shared Sub WriteFilesModif()
        Dim Compteur As Integer = 0
        Dim DataConfig As ConfigPerso = ConfigPerso.LoadConfig
        Dim ChaineFormattageNom As String = ""
        If DataConfig IsNot Nothing Then
            ChaineFormattageNom = DataConfig.FILESINFOS_ChaineFormattageNom
        End If
        Do
            Dim infos As tagID3FilesInfos = _ListeTacheAExecuter.First
            Dim MiseAJourID3V1 As Boolean
            _ListeTacheAExecuter.Remove(_ListeTacheAExecuter.First) ' A PASSER A LA FIN
            Try
                For Each NomComplet As String In infos.ListeFichiers
                    Compteur = Compteur + 1
                    Dim Info As New FileInfo(NomComplet)
                    If Not Info.IsReadOnly Then
                        Fenetre.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                                  New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                                     Path.GetFileName(NomComplet))
                        Select Case Path.GetExtension(NomComplet)
                            Case ".wav"
                                Dim FichierOrigine As String = NomComplet
                                infos.TabModif.ForEach(Sub(i As String)
                                                           Select Case i
                                                               Case "Nom"
                                                                   FileSystem.Rename(FichierOrigine, infos.Repertoire & "\" & infos.Nom & "." & infos.Extension)
                                                           End Select
                                                       End Sub)
                            Case ".mp3"
                                Using Fichier As New gbDev.TagID3.tagID3Object(NomComplet)
                                    infos.TabModif.ForEach(Sub(i As String)
                                                               Select Case i
                                                                   Case "Id3v2Tag"
                                                                       If Not infos.Id3v2Tag Then Fichier.ID3v2_Frames() = Nothing
                                                                   Case "Id3v1Tag"
                                                                       Fichier.ID3v1_Ok = infos.Id3v1Tag
                                                                       If Fichier.ID3v1_Ok = True Then MiseAJourID3V1 = True
                                                                   Case "TagNormalise"
                                                                       If infos.TagNormalise Then Fichier.NormaliseID3v2(False)
                                                                   Case "Nom"
                                                                       Fichier.RenameFile(infos.Nom & "." & infos.Extension)
                                                                   Case "Artiste"
                                                                       Fichier.ID3v2_Texte("TPE1") = TagID3.tagID3Object.FonctionUtilite.MiseEnFormeChaine(infos.Artiste)
                                                                   Case "Titre"
                                                                       Dim NouveauTitre = Replace(infos.Titre, "*", Fichier.ID3v2_Texte("TIT2"))
                                                                       Fichier.ID3v2_Texte("TIT2") = TagID3.tagID3Object.FonctionUtilite.MiseEnFormeChaine(NouveauTitre)
                                                                   Case "SousTitre"
                                                                       Fichier.ID3v2_TextePerso("VersionTitre") = infos.SousTitre
                                                                   Case "Album"
                                                                       Fichier.ID3v2_Texte("TALB") = TagID3.tagID3Object.FonctionUtilite.MiseEnFormeChaine(infos.Album)
                                                                   Case "Groupement"
                                                                       Fichier.ID3v2_Texte("TIT1") = infos.Groupement
                                                                   Case "Compositeur"
                                                                       Fichier.ID3v2_Texte("TCOM") = infos.Compositeur
                                                                   Case "Annee"
                                                                       Fichier.ID3v2_Texte("TYER") = infos.Annee
                                                                   Case "AnneeOrigine"
                                                                       Fichier.ID3v2_Texte("TORY") = infos.AnneeOrigine
                                                                   Case "Piste", "PisteTotale"
                                                                       Fichier.ID3v2_Texte("TRCK") = infos.Piste & "/" & infos.PisteTotale
                                                                   Case "Disque", "DisqueTotal"
                                                                       Fichier.ID3v2_Texte("TPOS") = infos.Disque & "/" & infos.DisqueTotal
                                                                   Case "Bpm"
                                                                       Fichier.ID3v2_Texte("TBPM") = infos.Bpm
                                                                   Case "Style"
                                                                       Fichier.ID3v2_Texte("TCON") = TagID3.tagID3Object.FonctionUtilite.MiseEnFormeChaine(infos.Style)
                                                                   Case "Label"
                                                                       Fichier.ID3v2_Texte("TPUB") = infos.Label
                                                                   Case "Catalogue"
                                                                       Fichier.ID3v2_Texte("TIT3") = infos.Catalogue
                                                                   Case "PageWeb"
                                                                       Fichier.ID3v2_Texte("WOAF") = infos.PageWeb
                                                                       infos.idRelease = Discogs.Get_ReleaseId(infos.PageWeb)
                                                                   Case "Compilation"
                                                                       Fichier.ID3v2_Texte("TCMP") = IIf(infos.Compilation, 1, 0).ToString
                                                                   Case "Commentaire"
                                                                       Fichier.ID3v2_Commentaire() = infos.Commentaire
                                                                   Case "Padding"
                                                                       If infos.Id3v2Tag Then
                                                                           If (infos.Padding < 0) Or (infos.Padding > 1000) Then infos.Padding = 1000
                                                                           Fichier.ID3v2_EXTHEADER_PaddingSize = infos.Padding
                                                                           Fichier.ID3v2Modifie = True
                                                                       End If
                                                                       '  Case "VinylCollection", "VinylDiscogs", "VinylWanted", "VinylForSale"
                                                                       '      Fichier.ID3v2_TextePerso("VinylCollection") = IIf(infos.VinylCollection, 1, 0).ToString & "/" & _
                                                                       '                                                       IIf(infos.VinylDiscogs, 1, 0).ToString & "/" & _
                                                                       '                                                       IIf(infos.VinylWanted, 1, 0).ToString & "/" & _
                                                                       '                                                       IIf(infos.VinylForSale, 1, 0).ToString & "/"
                                                               End Select
                                                           End Sub)
                                    'CALCUL de LA DUREE
                                    If MiseAJourID3V1 Then
                                        Fichier.ID3v1_Album = Fichier.ID3v2_Texte("TALB")
                                        Fichier.ID3v1_Annee = Fichier.ID3v2_Texte("TYER")
                                        Fichier.ID3v1_Artiste = Fichier.ID3v2_Texte("TPE1")
                                        Fichier.ID3v1_Genre = Fichier.ID3v2_Texte("TCON")
                                        Fichier.ID3v1_Titre = Fichier.ID3v2_Texte("TIT2")
                                    End If
                                    Dim j As fileMp3 = New fileMp3
                                    j.OpenFile(NomComplet, True)
                                    Dim WaveOctetParSec = j.WaveOctetParSec
                                    Dim DureeTotale = j.PlayDureeTotale
                                    Dim MinutesT As Integer = Int(DureeTotale / WaveOctetParSec / 60)
                                    Dim SecondesT As Integer = Int((DureeTotale / WaveOctetParSec) - MinutesT * 60)
                                    Dim Duree = String.Format("{0:D2}:{1:D2}", {MinutesT, SecondesT})
                                    Dim bitrate = j.GetMp3FrameHeader.Bitrate
                                    j.CloseFile()
                                    Fichier.ID3v2_Texte("TLEN") = Duree ' infos.Duree
                                    Fichier.ID3v2_TextePerso("idRelease") = infos.idRelease
                                    Fichier.ID3v2_TextePerso("Bitrate") = bitrate
                                    If Fichier.ID3v2_EXTHEADER_PaddingSize = 0 Then Fichier.ID3v2_EXTHEADER_PaddingSize = 200
                                    Fichier.SaveID3()
                                    Dim NouveauNom As String = Fichier.RenameFile("", ChaineFormattageNom, True)
                                    Dim ARenommer As Boolean = True
                                    For Each f In infos.ListeFichiers
                                        If Path.GetFileName(f) = NouveauNom Then
                                            ARenommer = False
                                            Exit For
                                        End If
                                    Next
                                    If ARenommer Then
                                        Debug.Print("renomme : " & infos.Nom & " en " & Fichier.RenameFile("", ChaineFormattageNom, True))
                                        Fichier.RenameFile("", ChaineFormattageNom)
                                    Else
                                        Debug.Print("Pas renomme : " & infos.Nom)
                                    End If
                                End Using
                        End Select
                    Else
                        Fenetre.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                                  New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                                  Path.GetFileName(NomComplet))
                    End If
                Next
            Catch ex As Exception
                Debug.Print("ERREUR EN COURS MODIF " & ex.Message)
            Finally
                infos.TabModif.Clear()
            End Try
        Loop While _ListeTacheAExecuter.Count > 0
        MaJTerminee = True
        Debug.Print("fin de la tache de fond")
        Fenetre.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
    End Sub
End Class
