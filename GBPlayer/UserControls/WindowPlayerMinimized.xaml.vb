'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/07/10 rev 04/08/10
'DESCRIPTION :Classe de lecteur audio
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Option Compare Text

Imports System.Windows.Controls.Primitives
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Threading
Imports System

'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class WindowPlayerMinimized
    '***********************************************************************************************
    '---------------------------------CONSTANTES PRIVEES DE LA CLASSE-------------------------------
    '***********************************************************************************************
    Private Const GBAU_NOMDOSSIER_TEMP = "GBDev\GBPlayer\Temp\"
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS DU CONTROLE
    Event FichierSuivant(ByVal NumPiste As Integer)
    Event FichierCharge(ByVal NomFichier As String)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DE LA FENETRE
    Private WithEvents Mixer As dxMixer
    Private RepDest As String
    Public Property ListePistes As List(Of PistePlayer) = New List(Of PistePlayer)
    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New(ByVal PisteEnCours As PistePlayer)
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        If PisteEnCours IsNot Nothing Then
            ListePistes.Add(PisteEnCours)
            PilePiste.Children.Add(PisteEnCours)
        End If
    End Sub
    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************

    '***********************************************************************************************
    '---------------------------------FONCTIONS PUBLIQUES DE LA CLASSE------------------------------
    '***********************************************************************************************
    Public Function GetPrincipalTrack() As PistePlayer
        If PilePiste.Children.Count > 0 Then
            Dim PisteEnCours As PistePlayer = PilePiste.Children.Item(0)
            PilePiste.Children.RemoveAt(0)
            Return PisteEnCours
        Else
            Return Nothing
        End If
    End Function
End Class

