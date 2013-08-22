'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'exposition public des énumérations passées en paramètre

Public Module BASS_Enum
    '***********************************************************************************************
    '-----------------------------ENUMERATIONS UTILISEES--------------------------------------------
    '***********************************************************************************************
    Public Enum Enum_Bass_CD_rwflag
        BASS_CD_RWFLAG_READCDR = 1
        BASS_CD_RWFLAG_READCDRW = 2
        BASS_CD_RWFLAG_READCDRW2 = 4
        BASS_CD_RWFLAG_READDVD = 8
        BASS_CD_RWFLAG_READDVDR = 16
        BASS_CD_RWFLAG_READDVDRAM = 32
        BASS_CD_RWFLAG_READANALOG = &H10000
        BASS_CD_RWFLAG_READM2F1 = &H100000
        BASS_CD_RWFLAG_READM2F2 = &H200000
        BASS_CD_RWFLAG_READMULTI = &H400000
        BASS_CD_RWFLAG_READCDDA = &H1000000
        BASS_CD_RWFLAG_READCDDASIA = &H2000000
        BASS_CD_RWFLAG_READSUBCHAN = &H4000000
        BASS_CD_RWFLAG_READSUBCHANDI = &H8000000
        BASS_CD_RWFLAG_READC2 = &H10000000
        BASS_CD_RWFLAG_READISRC = &H20000000
        BASS_CD_RWFLAG_READUPC = &H40000000
    End Enum
    Public Enum Enum_Bass_StreamCreate
        BASS_SAMPLE_FLOAT = 256        ' 32-bit floating-point
        BASS_SAMPLE_MONO = 2           ' mono
        BASS_SAMPLE_LOOP = 4           ' looped
        BASS_SAMPLE_3D = 8             ' 3D functionality
        BASS_SAMPLE_SOFTWARE = 16      ' not using hardware mixing
        BASS_SAMPLE_FX = 128           ' old implementation of DX8 effects
        BASS_STREAM_PRESCAN = &H20000   ' enable pin-point seeking/length (MP3/MP2/MP1)
        BASS_STREAM_AUTOFREE = &H40000 ' automatically free the stream when it stop/ends
        BASS_STREAM_DECODE = &H200000  ' don't play the stream, only decode (BASS_ChannelGetData)
        BASS_SPEAKER_FRONT = &H1000000 ' front speakers
        BASS_SPEAKER_REAR = &H2000000  ' rear/side speakers
        BASS_SPEAKER_CENLFE = &H3000000 ' center & LFE speakers (5.1)
        BASS_SPEAKER_REAR2 = &H4000000 ' rear center speakers (7.1)
        BASS_SPEAKER_LEFT = &H10000000 ' modifier: left
        BASS_SPEAKER_RIGHT = &H20000000 ' modifier: right
        BASS_SPEAKER_FRONTLEFT = BASS_SPEAKER_FRONT Or BASS_SPEAKER_LEFT
        BASS_SPEAKER_FRONTRIGHT = BASS_SPEAKER_FRONT Or BASS_SPEAKER_RIGHT
        BASS_SPEAKER_REARLEFT = BASS_SPEAKER_REAR Or BASS_SPEAKER_LEFT
        BASS_SPEAKER_REARRIGHT = BASS_SPEAKER_REAR Or BASS_SPEAKER_RIGHT
        BASS_SPEAKER_CENTER = BASS_SPEAKER_CENLFE Or BASS_SPEAKER_LEFT
        BASS_SPEAKER_LFE = BASS_SPEAKER_CENLFE Or BASS_SPEAKER_RIGHT
        BASS_SPEAKER_REAR2LEFT = BASS_SPEAKER_REAR2 Or BASS_SPEAKER_LEFT
        BASS_SPEAKER_REAR2RIGHT = BASS_SPEAKER_REAR2 Or BASS_SPEAKER_RIGHT
        BASS_UNICODE = &H80000000
    End Enum
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************

End Module
