Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 26/12/11
'DESCRIPTION :Classe fichier audio - classe de base des fichiers mp3 et wave
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports Microsoft.DirectX
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.IO

Public MustInherit Class fileAudio
    Friend FichierAudio As fileBinary = New fileBinary      'Fichier systeme pour les operations sur le fichier
    Friend InitTailleBuffer As Long = 23520    'Taille d'initialisation du buffer de lecture

    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        If FichierAudio IsNot Nothing Then FichierAudio.CloseFile()
        MyBase.Finalize()
    End Sub
    '***********************************************************************************************
    '----------------------------PROPRIETES DE LA CLASSE EN LECTURE/ECRITURE------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------LECTURE DES PROPRIETES DE LA CLASSE--------------------------------
    '***********************************************************************************************
    '--------------------------Retourne le nombre de canaux------------------------------
    Public MustOverride ReadOnly Property WaveNbrDeCanaux() As Int16
    '--------------------------Retourne la frequence -------------------------------------
    Public MustOverride ReadOnly Property WaveFrequence() As Int32
    '--------------------------Retourne le taux de compression---------------------------
    Public MustOverride ReadOnly Property WaveTauxCompression() As String
    '--------------------------Retourne le nombre d'octets par seconde -------------------
    Public MustOverride ReadOnly Property WaveOctetParSec() As Int32
    '--------------------------Retourne le nombre d'octets par échantillon ---------------
    Public MustOverride ReadOnly Property WaveOctetParEchant() As Int16
    '--------------------------Retourne le nombre de bits par echantillon ----------------
    Public MustOverride ReadOnly Property WaveBitsParEchant() As Int16
    '--------------------------Retourne le nombre d'octets du fichier-------------------------------
    Public Overridable ReadOnly Property FileSize() As Integer
        Get
            If FichierAudio IsNot Nothing Then Return FichierAudio.FileSize Else Return 0
        End Get
    End Property
    '----------------------Retourne TRUE si le fichier est ouvert-----------------------------------
    Public Overridable ReadOnly Property FileIsOpen() As Boolean
        Get
            If FichierAudio IsNot Nothing Then Return FichierAudio.FileIsOpen Else Return False
        End Get
    End Property
    '----------------------Lecture du nom du fichier------------------------------------------------
    Public Overridable ReadOnly Property FileName() As String
        Get
            If FichierAudio IsNot Nothing Then Return FichierAudio.FileName Else Return ""
        End Get
    End Property
    '----------------------Lecture du nom du fichier------------------------------------------------
    Public Overridable ReadOnly Property FilePosition As Long
        Get
            If FichierAudio IsNot Nothing Then Return FichierAudio.PositionPointer Else Return 0
        End Get
    End Property
    '----------------------Lecture de la taille du buffer de lecture--------------------------------
    Public ReadOnly Property PlayBufferSize() As Integer
        Get
            Return InitTailleBuffer
        End Get
    End Property
    '----------------------Retourne la duree totale du fichier--------------------------------------
    Public MustOverride ReadOnly Property PlayDureeTotale() As Integer
    '***********************************************************************************************
    '-------------------------------METHODES PUBLIQUES DE LA CLASSE---------------------------------
    '***********************************************************************************************
    '----------------------Fixe la position de lecture à venir--------------------------------------
    Public MustOverride Function SetNextPlayPosition(ByVal Pos As Long) As Long
    '----------------------Fonction de création d'un fichier audio-----------------------------------
    Public MustOverride Function CreateFile(ByVal Name As String) As Boolean
    '----------------------Fonction d'ouverture du fichier WAVE-------------------------------------
    Public MustOverride Function OpenFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
    '----------------------Fonction de fermeture du fichier WAVE------------------------------------
    Public MustOverride Function CloseFile() As Boolean
    '----------------------Fonction de lecture des données--------------------------------------
    Public MustOverride Function ReadWaveData(ByVal TailleBuffer As Long) As Byte()
    '----------------------Fonction d'écriture des données-----------------------------------
    Public MustOverride Function WriteData(ByVal Buffer() As Byte) As Boolean

End Class
