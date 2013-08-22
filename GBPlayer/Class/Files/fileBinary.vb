'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 18/07/10 rev: 03/08/10 (Ajout des exceptions transmises lors des echecs)
'DESCRIPTION :Classe de lecture ecriture dans un fichier Windows
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
'***********************************************************************************************
'-------------------------------CONSTANTES UTILISEES EN INTERNE---------------------------------
'***********************************************************************************************
'***********************************************************************************************
'---------------------------------TYPES PRIVEES DE LA CLASSE------------------------------------
'***********************************************************************************************
Class fileBinary
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private strFileName As String       'Nom du fichier
    Private FileObject As FileStream    'Handle du fichier ouvert
    Private Retour As Long              'Valeur de retour des fonctions
    Private SizeIOData As Long          'Quantité de données écrites ou lues
    '***********************************************************************************************
    '---------------------------------CONSTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Public Sub New() 'Constructeur d'une classe
        FileObject = Nothing
        strFileName = ""
    End Sub
    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        Call CloseFile()
        MyBase.Finalize()
    End Sub
    '***********************************************************************************************
    '----------------------------LECTURE DES PROPRIETES DE LA CLASSE--------------------------------
    '***********************************************************************************************
    '-------------------------------Retourne le nom du fichier--------------------------------------
    Public ReadOnly Property FileName() As String
        Get
            FileName = strFileName
        End Get
    End Property
    '-------------------------------Retourne le handle du fichier-----------------------------------
    Public ReadOnly Property FileHandle() As FileStream
        Get
            FileHandle = FileObject
        End Get
    End Property
    '--------------------------Retourne si le fichier est ouvert------------------------------------
    Public ReadOnly Property FileIsOpen() As Boolean
        Get
            FileIsOpen = (FileObject IsNot Nothing And strFileName <> "")
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets du fichier-------------------------------
    Public ReadOnly Property FileSize() As Long
        Get
            Dim InfoFichier As FileInfo
            If strFileName <> "" Then
                InfoFichier = New FileInfo(strFileName)
                FileSize = InfoFichier.Length()
            Else
                FileSize = 0
            End If
        End Get
    End Property
    '--------------------------Force l'envoi d'une exception lors d'une erreur----------------------
    Public Property NoException As Boolean = True
    '***********************************************************************************************
    '----------------------------ECRITURE DES PROPRIETES DE LA CLASSE-------------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '------------------------------METHODES PUBLIQUES DE LA CLASSE----------------------------------
    '***********************************************************************************************
    '--------------------------Création du fichier--------------------------------------------------
    Public Function CreateNewFile(ByVal Name As String) As Boolean
        If Not FileIsOpen Then
            If Name <> "" Then
                Try
                    FileObject = New FileStream(Name, FileMode.Create)
                    strFileName = Name
                    Return True
                Catch ex As IOException
                    FileObject = Nothing
                    strFileName = ""
                    If Not NoException Then Throw New Exception("Echec lors de la creation du fichier", ex)
                End Try
            End If
        End If
        Return False
    End Function
    '--------------------------Ouverture du fichier-------------------------------------------------
    Public Function OpenFile(ByVal Name As String, Optional ByVal Access As FileAccess = FileAccess.ReadWrite,
                             Optional ByVal SharedMode As FileShare = FileShare.ReadWrite) As Boolean
        If FileIsOpen Then CloseFile()
        If Name <> "" Then
            Try
                FileObject = New FileStream(Name, FileMode.Open, Access, SharedMode)
                strFileName = Name
                Return True
            Catch ex As Exception
                FileObject = Nothing
                strFileName = ""
                If Not NoException Then Throw New Exception("Echec lors de l'ouverture du fichier", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Fermeture du fichier-------------------------------------------------
    Public Function CloseFile() As Boolean
        If FileIsOpen Then
            FlushBuffers()
            FileObject.Close()
            strFileName = ""
            FileObject = Nothing
            Return True
        End If
        Return False
    End Function
    '--------------------------Ecriture d'une zone de données---------------------------------------
    Public Function WriteData(ByVal Buffer As Long, ByVal TailleBuffer As Integer) As Boolean
        If FileIsOpen Then
            Try
                Dim Mem(TailleBuffer - 1) As Byte
                Marshal.Copy(Buffer, Mem, 0, TailleBuffer)
                FileObject.Write(Mem, 0, TailleBuffer)
                WriteData = True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'un entier 16 bits----------------------------------------
    Public Function WriteInt16(ByVal Data As Int16) As Boolean
        If FileIsOpen Then
            Try
                Dim TabInt16(1) As Byte
                TabInt16(1) = CByte((Data >> 8) And &HFF)
                TabInt16(0) = CByte(Data And &HFF)
                FileObject.Write(TabInt16, 0, TabInt16.Length)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'un entier 16 bits----------------------------------------
    Public Function WriteInt32(ByVal Data As Int32) As Boolean
        If FileIsOpen Then
            Try
                Dim TabInt32(3) As Byte
                TabInt32(3) = CByte((Data >> 24) And &HFF)
                TabInt32(2) = CByte((Data >> 16) And &HFF)
                TabInt32(1) = CByte((Data >> 8) And &HFF)
                TabInt32(0) = CByte(Data And &HFF)
                FileObject.Write(TabInt32, 0, TabInt32.Length)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'une zone de données----------------------------------------
    Public Function WriteData(ByVal Buffer() As Byte) As Boolean
        If FileIsOpen Then
            Try
                FileObject.Write(Buffer, 0, Buffer.Length)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'un BYTE de donnée-----------------------------------------
    Public Function WriteByte(ByVal Buffer As Byte) As Boolean
        If FileIsOpen Then
            Try
                FileObject.WriteByte(Buffer)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'une chaine de caractères----------------------------------
    '--------------------------Ecriture d'une chaine de caractères----------------------------------
    Public Function WriteString(ByVal Buffer As String) As Boolean
        If FileIsOpen Then
            Try
                Dim encoding As New ASCIIEncoding()
                If Buffer Is Nothing Then Buffer = ""
                FileObject.Write(encoding.GetBytes(Buffer), 0, Buffer.Length)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Ecriture d'une chaine de caractères----------------------------------
    Public Function WriteStringU(ByVal Buffer As String) As Integer
        If FileIsOpen Then
            Try
                Dim encoding As New UnicodeEncoding()
                If Buffer Is Nothing Then Buffer = ""
                Dim TabCar As Byte() = encoding.GetBytes(Buffer)
                FileObject.Write(TabCar, 0, TabCar.Length)
                Return TabCar.Length
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    Public Function WriteCString(ByVal Buffer As String) As Boolean
        If FileIsOpen Then
            Try
                Dim encoding As New ASCIIEncoding()
                If Buffer Is Nothing Then Buffer = ""
                FileObject.Write(encoding.GetBytes(Buffer & Chr(0)), 0, Buffer.Length + 1)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    Public Function WriteString(ByVal Buffer As String, ByVal Taille As Integer) As Boolean
        If FileIsOpen Then
            Try
                Dim TabWrite(Taille - 1) As Byte
                If Buffer IsNot Nothing Then
                    Dim encoding As New ASCIIEncoding()
                    Array.Copy(encoding.GetBytes(Buffer), TabWrite, Math.Min(Buffer.Length, Taille))
                End If
                FileObject.Write(TabWrite, 0, TabWrite.Length)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de l'écriture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Lecture d'une zone de données----------------------------------------
    Public Function ReadData(ByVal Buffer As IntPtr, ByRef TailleBuffer As Long) As Boolean
        If FileIsOpen Then
            Try
                Dim TabByte(TailleBuffer) As Byte
                TailleBuffer = FileObject.Read(TabByte, 0, TailleBuffer)
                Marshal.Copy(TabByte, 0, Buffer, TailleBuffer)
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de la lecture des données", ex)
            End Try
        End If
        Return False
    End Function
    '--------------------------Lecture d'une zone de données----------------------------------------
    Public Function ReadData(ByRef TailleBuffer As Long) As Byte()
        If FileIsOpen Then
            Dim Buffer(TailleBuffer - 1) As Byte
            Try
                Dim TailleRetour As Long = FileObject.Read(Buffer, 0, TailleBuffer)
                If TailleRetour < TailleBuffer Then
                    Dim TabRetour(TailleRetour - 1) As Byte
                    Array.Copy(Buffer, TabRetour, TailleRetour)
                    TailleBuffer = TailleRetour
                    Return TabRetour
                Else
                    Return Buffer
                End If
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de la lecture des données", ex)
            End Try
        End If
        Return Nothing
    End Function
    '--------------------------Lecture d'un BYTE de donnée------------------------------------------
    Public Function ReadByte() As Byte
        Dim Buffer As Byte
        If FileIsOpen Then
            Try
                Buffer = FileObject.ReadByte()
                Return Buffer
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de la lecture des données", ex)
            End Try
        End If
        Return 0
    End Function
    '--------------------------Lecture d'un mot-----------------------------------------------------
    Public Function ReadMot() As Int16
        Dim TempBuffer As Int16
        TempBuffer = ReadByte()
        Return TempBuffer + ReadByte() * 16 ^ 2
    End Function
    '--------------------------Lecture d'un double mot----------------------------------------------
    Public Function ReadDMot() As Integer
        Dim TempBuffer As Int32
        TempBuffer = ReadByte()
        TempBuffer = TempBuffer + ReadByte() * 16 ^ 2
        TempBuffer = TempBuffer + ReadByte() * 16 ^ 4
        Return TempBuffer + ReadByte() * 16 ^ 6
    End Function
    '--------------------------Lecture d'un double inverse------------------------------------------
    Public Function ReadDMotInverse() As Integer
        Dim TempBuffer As Int32
        TempBuffer = ReadByte() * 16 ^ 6
        TempBuffer = TempBuffer + ReadByte() * 16 ^ 4
        TempBuffer = TempBuffer + ReadByte() * 16 ^ 2
        Return TempBuffer + ReadByte()
    End Function
    '--------------------------Lecture d'une chaine de caractères-----------------------------------
    Public Function ReadString(ByVal TailleChaine As Integer, Optional ByVal ConserverChr0 As Boolean = False,
                               Optional ByVal unicode As Boolean = False) As String
        If TailleChaine >= 0 And FileIsOpen Then
            Try
                Dim dBytes(TailleChaine - 1) As Byte
                Dim uni As New UnicodeEncoding()
                If FileObject.Read(dBytes, 0, TailleChaine) > 0 Then
                    If ConserverChr0 Then
                        If unicode Then Return uni.GetString(dBytes) Else 
                        Return Text.Encoding.ASCII.GetString(dBytes)
                    Else
                        If unicode Then
                            If (dBytes(0) = 255) And (dBytes(1) = 254) Then
                                Dim Chaine As String = ExtraitChaine(uni.GetString(dBytes), "", Chr(0), , True)
                                Return Right(Chaine, Chaine.Length - 1)
                            Else
                                Return ExtraitChaine(uni.GetString(dBytes), "", Chr(0), , True)
                            End If
                        Else
                            Return ExtraitChaine(Text.Encoding.ASCII.GetString(dBytes), "", Chr(0), , True)
                        End If
                    End If
                End If
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de la lecture des données", ex)
            End Try
        End If
        Return ""
    End Function
    '--------------------------Lecture d'une chaine de caractères tye c-----------------------------
    Public Function ReadString() As String
        If FileIsOpen Then
            Try
                Dim dByte As Byte
                Dim ChaineRetour As String = ""
                Do
                    dByte = FileObject.ReadByte()
                    If dByte <> 0 Then
                        ChaineRetour = ChaineRetour & Chr(dByte)
                    End If
                Loop Until dByte = 0
                Return ChaineRetour
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors de la lecture des données", ex)
            End Try
        End If
        Return ""
    End Function
    '--------------------------Changer la position de lecture et d'ecriture-------------------------
    Public Function ChangePointer(ByVal Position As Long, Optional ByVal PtDepart As System.IO.SeekOrigin = SeekOrigin.Begin) As Long
        If FileIsOpen Then
            Dim MemPosition As Long = FileObject.Position
            Try
                FileObject.Seek(Position, PtDepart)
                If FileObject.Position <> MemPosition Then Return FileObject.Position
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec lors du positionnement du pointer dans le fichier", ex)
            End Try
        End If
        Return -1
    End Function
    Public Function PositionPointer() As Long
        If FileIsOpen Then Return FileObject.Position Else Return 0
    End Function
    '----------------Force l'ecriture sur le disque des données bufferisées-------------------------
    Public Function FlushBuffers() As Boolean
        If FileIsOpen Then
            Try
                FileObject.Flush()
                Return True
            Catch ex As Exception
                If Not NoException Then Throw New Exception("Echec du flush du buffer fichier", ex)
            End Try
        End If
        Return False
    End Function
    '***********************************************************************************************
    '-------------------------------METHODES PRIVEES DE LA CLASSE-----------------------------------
    '***********************************************************************************************
End Class
