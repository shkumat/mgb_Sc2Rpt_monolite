// Версия 1.23 от 22 августа 2018г. Построитель отчетов в формате Скрудж-2
// encoding=cp-1251
/*
		Параметры программы :

	-DayDate	Отчетная дата - целое число например 42624 ,
			если не указано, то отчетная дата = сегодня ;
	-mode		Код отчета, который нужно построить
		ccda	ClientBank--Credit-reestr--Daily--All
			ежеднеаная клиент-банковская кредитовка по всем ;
		ceda	ClientBank--Extract--Daily--All
			ежеднеаная клиент-банковская кредитовка по всем ;
		beda	Bank--Extract--Daily--All
			ежедневная банковская выписка по всем ;
		bemc	Bank--Extract--Monthly--Currency
			ежемесячная банковская выписка по валютным счетам;
		bdda	Bank--Documents--Daily--All
			ежедневный реестр банковских документов дня ;
		bsda	Bank--Saldo--Daily--All
			ежедневная сальдовка по всем счетам ;
		bbds	Bank--Balance--Daily--Simple
			ежедневный банковский баланс ( простой )
		bbdc	Bank--Balance--Daily--Consolidated
			ежедневный банковский баланс ( сводный )
		e2col	divide Extract for 2 Columns
			разбить банковские выписки на 2 колонки
		#01	отчетный файл #01
		#02	отчетный файл #02
*/
using	__	=	MyTypes.CCommon ;
using	money	=	System.Decimal	;
using	MyTypes;

public	class	Sc2Rpt {
	static	string	ScroogeDir	=	"";
	static	string	ScroogeOut	=	"";
	static	string	ServerName	=	"";
	static	string	DataBase	=	"";
	static	string	ConnectionString=	CAbc.EMPTY ;
	static	readonly int YUZHCABLE_ID=	1011727	;
	static	CConnection Connection			;
	static	int	TODAY		=	CCommon.Today()	;
	static	int	DefaultDateFrom	=	GetFirstDayOfMonth( TODAY ) ;
	static	int	DefaultDateInto	=	GetLastDayOfMonth( TODAY ) ;
	static	string	DATE_FROM_STR	=	CCommon.StrD( DefaultDateFrom , 10,10).Substring(6)
					+	CCommon.StrD( DefaultDateFrom , 10,10).Substring(2,4)
					+	CCommon.StrD( DefaultDateFrom , 10,10).Substring(0,2);
	static	string	DATE_INTO_STR	=	CCommon.StrD( DefaultDateInto , 10,10).Substring(6)
					+	CCommon.StrD( DefaultDateInto , 10,10).Substring(2,4)
					+	CCommon.StrD( DefaultDateInto , 10,10).Substring(0,2);

	static	void	PrintAboutMe() {//FOLD01
		__.Print(""," Построитель отчетов в формате Скрудж-2. Версия 1.22 от 22.08.2018г.");
		__.Print("\t\t\tПараметры программы :");
		__.Print("\t-DayDate\tОтчетная дата - целое число например 42624 ,");
		__.Print("\t\t\tесли не указано, то отчетная дата = сегодня ;");
		__.Print("\t-mode\t\tКакой отчет строить");
		__.Print("\t\tccda\tClientBank--Credit-reestr--Daily--All");
		__.Print("\t\t\tежеднеаная клиент-банковская кредитовка по всем ;");
		__.Print("\t\tceda\tClientBank--Extract--Daily--All");
		__.Print("\t\t\tежедневная клиент-банковская выписка по всем ;");
		__.Print("\t\tbeda\tBank--Extract--Daily--All");
		__.Print("\t\t\tежеднеаная банковская выписка по всем ;");
		__.Print("\t\tbemc\tBank--Extract--Monthly--Currency");
		__.Print("\t\t\tежемесячная банковская выписка по валютным счетам;");
		__.Print("\t\tbdda\tBank--Documents--Daily--All");
		__.Print("\t\t\tежеднеаный реестр банковских документов дня ;");
		__.Print("\t\tbbds\tBank--Balance--Daily--Simple");
		__.Print("\t\t\tежедневный банковский баланс (простой) ;");
		__.Print("\t\tbbdc\tBank--Balance--Daily--Consolidated");
		__.Print("\t\t\tежедневный банковский баланс (сводный) ;");
		__.Print("\t\tbsda\tBank--Saldo--Daily--All");
		__.Print("\t\t\tежедневная сальдовка по всем счетам ;");
		__.Print("\t\te2col\tdivide Extract for 2 Columns");
		__.Print("\t\t\tразбить банковские выписки на 2 колонки ;");
		__.Print("\t\t#01\tотчетный файл #01 ;");
		__.Print("\t\t#02\tотчетный файл #02 ." , "");
		__.Print("  Пример : Sc2Rpt.exe  -DayDate 42542  -mode BDDA " );
	}//FOLD01
	//-----------------------------------------------------------------------
	// точка входа в программу
	//  Функцию Main нужно пометить атрибутом [STAThread], чтоб работал OpenFileBox
	[System.STAThread]
	static	void	Main()  {//FOLD01
		const	bool	DEBUG		=	false		;
		int		DayDate		=	0		;
		int		NextDate	=	0		;
		string		DayOutDir	=	CAbc.EMPTY
		,		LogFileName	=	CAbc.EMPTY	;
		CParam		Param		= new	CParam()	;
		if	( __.IsEmpty( Param["DAYDATE"] ) )
			DayDate		=	__.Today();
		else
			DayDate		=	__.CInt( Param["DAYDATE"] );
		if	( __.IsEmpty( Param["NEXTDATE"] ) )
			NextDate	=	__.Today()+1;
		else
			NextDate	=	__.CInt( Param["NEXTDATE"] );
		if	( ! DEBUG )
			if	( __.ParamCount() < 2 ) {
				PrintAboutMe();
				return;
			}
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CConsole.Color	=	CConsole.GRAY;
		CConsole.Clear();
		CCommon.Print( ""," Построитель отчетов в формате Скрудж-2. Версия 1.22 от 22.08.2018г." ,"") ;
		CScrooge2Config	Scrooge2Config	= new	CScrooge2Config();
		if (!Scrooge2Config.IsValid) {
			CCommon.Print( Scrooge2Config.ErrInfo ) ;
			return;
		}
		ScroogeDir	=	(string)Scrooge2Config["Root"].Trim();
		ScroogeOut	=	ScroogeDir.Trim() + "\\" + Scrooge2Config["Output"].Trim() + "\\";
		ServerName	=	(string)Scrooge2Config["Server"];
		DataBase	=	(string)Scrooge2Config["DataBase"];
		if( ScroogeDir == null ) {
			CCommon.Print("  Не найдена переменная `Root` в настройках `Скрудж-2` ");
			return;
		}
		if( ServerName == null ) {
			CCommon.Print("  Не найдена переменная `Server` в настройках `Скрудж-2` ");
			return;
		}
		if( DataBase == null ) {
			CCommon.Print("  Не найдена переменная `Database` в настройках `Скрудж-2` ");
			return;
		}
		CCommon.Print("  Беру настройки `Скрудж-2` здесь :  " + ScroogeDir );
		__.Print("  Сервер        :  " + ServerName  );
		__.Print("  База данных   :  " + DataBase + CAbc.CRLF );
		ConnectionString	=	"Server="	+	ServerName
					+	";Database="	+	DataBase
					+	";Integrated Security=TRUE;"  ;
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DayOutDir		=	ScroogeOut;
		if	(  DayOutDir !=	null ) {
			if	( ! CCommon.DirExists( DayOutDir ) )
				CCommon.MkDir( DayOutDir );
			if	( CCommon.DirExists( DayOutDir ) ) {
				DayOutDir	+=	"\\" + CCommon.StrD( DayDate , 8 , 8 ).Replace("/","").Replace(".","");
				if	( ! CCommon.DirExists( DayOutDir ) )
					CCommon.MkDir( DayOutDir );
				if	( ! CCommon.DirExists( DayOutDir ) )
					ScroogeOut	=	ScroogeDir + "\\" ;
				}
			LogFileName		=	DayOutDir + "\\" + "dayclose.log" ;
		}
		else
			LogFileName		=	ScroogeDir + "\\" + "dayclose.log" ;
		Err.LogTo( LogFileName );
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		switch	( Param["MODE"].ToUpper().Trim() ) {
			case	"BBDC": {
				BBDC( DayDate );
				break;
			}
			case	"BBDS": {
				BBDS( DayDate );
				break;
			}
			case	"BDDA": {
				BDDA( DayDate );
				break;
			}
			case	"BEDA": {
				BEDA( DayDate );
				break;
			}
			case	"BEMC": {
				BEMC();
				break;
			}
			case	"BSDA": {
				BSDA( DayDate );
				break;
			}
			case	"CEDA": {
				CEDA( DayDate );
				break;
			}
			case	"CCDA": {
				CCDA( DayDate );
				break;
			}
			case	"E2COL": {
				E2COL( DayDate );
				break;
			}
			case	"#01": {
				Repozitory( DayDate , NextDate , "01" );
				break;
			}
			case	"#02": {
				Repo02( DayDate , NextDate );
				break;
			}
			default	: {
				__.Print("Указан неправильный код отчета.");
				break;
			}
		}
		if	( DEBUG )
			BEMC();
	}//FOLD01
	static	string	DtoR( int DayDate ) {//fold01
		string	Result	=	__.DtoC( DayDate );
		Result		=	Result.Substring(6,2)
				+	Result.Substring(4,2)
				+	Result.Substring(0,4);
		return	Result;
	}//FOLD01
	//--------------------------------------------------------------------------
	// построение баланса #02 через собственную процедуру  Mega_Repozitory_02
	static	void	Repo02( int DayDate , int NextDate ) {//fold01
			CConnection	Connection	= new	CConnection( ConnectionString );
			if	( ! Connection.IsOpen() )  {
				Connection.Close();
				return;
			}
			int		LineCount		=	0
			;
			string	Bank_Code		=	"xxxxxx"
			,		Bank_EMail		=	"xxxx"
			,		MainCurrency	=	"980"
			;
			CRecordSet	BankInfo	= new	CRecordSet( Connection );
			if	( BankInfo.Open(" select EMail,Code,MainCrncyId from dbo.vMega_Common_MyBankInfo ") )
				if	( BankInfo.Read() ) {
					Bank_EMail		=		BankInfo["EMail"].Trim() ;
					Bank_Code		=		BankInfo["Code"].Trim() ;
					MainCurrency	=		BankInfo["MainCrncyId"].Trim();
				}
			BankInfo.Close();
			string	ShortFileName	=	"#02"
									+	__.Right("___" + Bank_EMail.ToUpper() , 3 )
									+	__.StrH( __.Month( DayDate ) , 1  )
									+	__.StrY( __.Day( DayDate ) , 1  )
									+	".C"
									+	__.StrY( __.Day( NextDate ) , 1 )
									+	"1";
			string	Header			=	"03"
									+	"=" +	DtoR( NextDate )
									+	"=" +	DtoR( DayDate )
									+	"=" +	DtoR( DayDate )
									+	"=" +	DtoR( __.Today() )
									+	"=" + __.Hour( __.Now() ).ToString("00") + __.Minute( __.Clock() ).ToString("00")
									+	"=" + __.Left( Bank_Code , 6 )
									+	"=" + "21"
									+	"=" + LineCount.ToString("000000000")
									+	"=" +	__.Left( ShortFileName , 12 )
									+	"=      =";
			string	FileName		=	ScroogeOut + CAbc.SLASH + ShortFileName ;
			__.Print("Вывожу отчет в файл " + FileName);
			 string	Output			=	""
			 ,		Topic			=	""
			 ,		Currency		=	""
			 ,		Resident		=	""
			 ;
			 long   MainDebet   	=	0
			 ,		MainCredit		=	0
			 ,		CrncyDebet      =	0
			 ,		CrncyCredit     =	0
			 ,		MainCorDebet	=	0
			 ,		MainCorCredit	=	0
			 ,		CrncyCorDebet	=	0
			 ,		CrncyCorCredit	=	0
			 ,		MainCorrDebet	=	0
			 ,		MainCorrCredit	=	0
			 ,		CrncyCorrDebet	=	0
			 ,		CrncyCorrCredit	=	0
			 ,		MainActive      =	0
			 ,		MainPassive     =	0
			 ,		CrncyActive     =	0
			 ,		CrncyPassive	=	0;
			 CRecordSet	RecordSet	= new	CRecordSet( Connection );
			 if	( RecordSet.Open("Exec dbo.Mega_Repozitory_02;3 " + DayDate.ToString() + " , " + DayDate.ToString() + " , 0 , " + MainCurrency ) )
				if	( RecordSet.Read()  ) {
					CTextWriter	TextWriter	= new	CTextWriter();
					TextWriter.OpenForAppend( FileName , CAbc.CHARSET_DOS ) ;
					TextWriter.Add( __.Left(CAbc.EMPTY,100) + CAbc.CRLF );
					TextWriter.Add( __.Left( Header , 148 ) + CAbc.CRLF ) ;
					TextWriter.Add( "#1=" + Bank_Code + CAbc.CRLF ) ;
					do {
						 Output         =   "";
						 Topic          =   RecordSet[ 0 ].ToString().Trim();
						 Currency       =   RecordSet[ 1 ].ToString().Trim();
						 Resident       =	RecordSet[ 2 ].ToString().Trim();
						 MainDebet      =   __.CInt64( RecordSet[ 3 ].ToString().Trim() ) ;
						 MainCredit     =   __.CInt64( RecordSet[ 4 ].ToString().Trim() ) ;
						 CrncyDebet     =   __.CInt64( RecordSet[ 5 ].ToString().Trim() ) ;
						 CrncyCredit    =   __.CInt64( RecordSet[ 6 ].ToString().Trim() ) ;
						 MainCorDebet   =   __.CInt64( RecordSet[ 7 ].ToString().Trim() ) ;
						 MainCorCredit  =   __.CInt64( RecordSet[ 8 ].ToString().Trim() ) ;
						 CrncyCorDebet  =   __.CInt64( RecordSet[ 9 ].ToString().Trim() ) ;
						 CrncyCorCredit =   __.CInt64( RecordSet[ 10 ].ToString().Trim() ) ;
						 MainCorrDebet  =   __.CInt64( RecordSet[ 11 ].ToString().Trim() ) ;
						 MainCorrCredit =   __.CInt64( RecordSet[ 12 ].ToString().Trim() ) ;
						 CrncyCorrDebet =   __.CInt64( RecordSet[ 13 ].ToString().Trim() ) ;
						 CrncyCorrCredit=   __.CInt64( RecordSet[ 14 ].ToString().Trim() ) ;
						 MainActive     =   __.CInt64( RecordSet[ 15 ].ToString().Trim() ) ;
						 MainPassive    =   __.CInt64( RecordSet[ 16 ].ToString().Trim() ) ;
						 CrncyActive    =   __.CInt64( RecordSet[ 17 ].ToString().Trim() ) ;
						 CrncyPassive   =   __.CInt64( RecordSet[ 18 ].ToString().Trim() ) ;
						 if ( Currency == MainCurrency ) {
							if  ( CrncyActive > 0 )
							   Output += "10" + Topic + MainCurrency + Resident + "=" + CrncyActive.ToString().Trim() + CAbc.CRLF;
							if ( CrncyPassive > 0 )
							   Output += "20" + Topic + MainCurrency + Resident + "=" + CrncyPassive.ToString().Trim() + CAbc.CRLF;
							if  ( CrncyDebet > 0 )
							   Output += "50" + Topic + MainCurrency + Resident + "=" + CrncyDebet.ToString().Trim() + CAbc.CRLF;
							if  ( CrncyCredit > 0 )
							   Output += "60" + Topic + MainCurrency + Resident + "=" + CrncyCredit.ToString().Trim() + CAbc.CRLF;
							if ( CrncyCorDebet > 0 )
							   Output += "70" + Topic + MainCurrency + Resident + "=" + CrncyCorDebet.ToString().Trim() + CAbc.CRLF;
							if ( CrncyCorCredit > 0 )
							   Output += "80" + Topic + MainCurrency + Resident + "=" + CrncyCorCredit.ToString().Trim() + CAbc.CRLF;
							if ( CrncyCorrDebet > 0 )
							   Output += "90" + Topic + MainCurrency + Resident + "=" + CrncyCorrDebet.ToString().Trim() + CAbc.CRLF;
							if ( CrncyCorrCredit > 0 )
							   Output += "00" + Topic + MainCurrency + Resident + "=" + CrncyCorrCredit.ToString().Trim() + CAbc.CRLF;
						 } else {
							if ( MainActive > 0 )
							   Output += "10" + Topic + Currency + Resident + "=" + MainActive.ToString().Trim()  + CAbc.CRLF;
							if ( MainPassive > 0 )
							   Output += "20" + Topic + Currency + Resident + "=" + MainPassive.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyActive > 0 )
							   Output += "11" + Topic + Currency + Resident + "=" + CrncyActive.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyPassive > 0 )
							   Output += "21" + Topic + Currency + Resident + "=" + CrncyPassive.ToString().Trim()  + CAbc.CRLF;
							if ( MainDebet > 0 )
							   Output += "50" + Topic + Currency + Resident + "=" + MainDebet.ToString().Trim()  + CAbc.CRLF;
							if ( MainCredit > 0 )
							   Output += "60" + Topic + Currency + Resident + "=" + MainCredit.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyDebet > 0 )
							   Output += "51" + Topic + Currency + Resident + "=" + CrncyDebet.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyCredit > 0 )
							   Output += "61" + Topic + Currency + Resident + "=" + CrncyCredit.ToString().Trim()  + CAbc.CRLF;
							if ( MainCorDebet > 0 )
							   Output += "70" + Topic + Currency + Resident + "=" + MainCorDebet.ToString().Trim()  + CAbc.CRLF;
							if ( MainCorCredit > 0 )
							   Output += "80" + Topic + Currency + Resident + "=" + MainCorCredit.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyCorDebet > 0 )
							   Output += "71" + Topic + Currency + Resident + "=" + CrncyCorDebet.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyCorCredit > 0 )
							   Output += "81" + Topic + Currency + Resident + "=" + CrncyCorCredit.ToString().Trim()  + CAbc.CRLF;
							if ( MainCorrDebet > 0 )
							   Output += "90" + Topic + Currency + Resident + "=" + MainCorrDebet.ToString().Trim()  + CAbc.CRLF;
							if ( MainCorrCredit > 0 )
							   Output += "00" + Topic + Currency + Resident + "=" + MainCorrCredit.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyCorrDebet > 0 )
							   Output += "91" + Topic + Currency + Resident + "=" + CrncyCorrDebet.ToString().Trim()  + CAbc.CRLF;
							if ( CrncyCorrCredit > 0 )
							   Output += "01" + Topic + Currency + Resident + "=" + CrncyCorrCredit.ToString().Trim()  + CAbc.CRLF;
						 }
						 if	( ! TextWriter.Add( Output ) )
							break;
					  } while    ( RecordSet.Read() );
					  TextWriter.Close();
				}
			RecordSet.Close();
			Connection.Close();
			__.Print("Готово. Для продолжения нажмите Enter.");
			CConsole.ClearKeyboard();
			CConsole.Flash();
			CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// построение оборотно-сальдовой ведомости по всем счетам ( делалось для филиала )
	static	void	BSDA( int DayDate  ) {//fold01
		CSc2Reports	Sc2Reports	= new	CSc2Reports();
		string		FileName	=	ScroogeOut + "\\" + __.StrD( DayDate , 8 , 8 ).Replace(".","").Replace("/","")+".sld";
		if	( Sc2Reports.Open( ConnectionString ) ) {
			__.Print("Вывожу отчет в файл "+FileName);
			Sc2Reports.Saldovka(	FileName
					,	DayDate	// FromDate
					,	DayDate	// ToDate
					,	0	// CorDate
					,	"%"	// Code
					,	""	// ClientCode
					,	""	// CurrencyTag
					,	0	// BranchId
					,	0	// GroupId
					,	0	// UserId
					,	2	// HideFlag
				);
		}
		Sc2Reports.Close();
		__.Print("Готово. Для продолжения нажмите Enter.");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// построение документов дня
	static	void	BDDA( int DayDate  ) {//fold01
		CSc2Reports	Sc2Reports	= new	CSc2Reports();
		string		FileName	=	ScroogeOut + "\\" + __.StrD( DayDate , 8 , 8 ).Replace(".","").Replace("/","")+".doc";
		if	( Sc2Reports.Open( ConnectionString ) ) {
			__.Print("Вывожу отчет в файл "+FileName);
			Sc2Reports.DocOfDay( DayDate , FileName );
		}
		Sc2Reports.Close();
		__.Print("Готово. Для продолжения нажмите Enter.");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// построение репозитарного файла
	static	void	Repozitory( int DayDate , int NextDate , string FileCode ) {//fold01
		CSc2Reports	Sc2Reports		= new	CSc2Reports();
		if	( Sc2Reports.Open( ConnectionString ) )
			if	(	// Для головного банка #01 по схеме D
					( Sc2Reports.Branch_Kind == 0 )
				&&	( ( FileCode=="1" ) || ( FileCode=="01" ) )
				)
				Sc2Reports.Repozitory( FileCode , ScroogeOut , DayDate , DayDate , NextDate , true , true );
			else
				Sc2Reports.Repozitory( FileCode , ScroogeOut , DayDate , DayDate , NextDate , false , true );
		Sc2Reports.Close();
		__.Print("Готово. Для продолжения нажмите Enter.");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// добавление одного текстового файла в конец другого файла
	static	void	Append( string TargetFileName , string SrcFileName ) {//fold01
		if	( ( SrcFileName == null ) || ( TargetFileName == null ) )
			return;
		SrcFileName	=	SrcFileName.Trim();
		TargetFileName	=	TargetFileName.Trim();
		if	( ( SrcFileName.Length == 0 ) || ( TargetFileName.Length == 0 ) )
			return;
		CTextWriter	TextWriter	= new	CTextWriter();
		CTextReader	TextReader	= new	CTextReader();
		TextWriter.OpenForAppend( TargetFileName , CAbc.CHARSET_DOS ) ;
		if	( TextReader.Open( SrcFileName , CAbc.CHARSET_DOS ) )
			while	( TextReader.Read() )
				if	( ! TextWriter.Add( TextReader.Value , CAbc.CRLF ) )
					break;
		TextReader.Close();
                TextWriter.Close();
	}//FOLD01
	//--------------------------------------------------------------------------
	// разбиение файла с выпиской на две колонки
	static	void	Extract2Columns( string FileName , string TargetFileName ) {//fold01
		if	(	( FileName == null )
			||	( TargetFileName == null )
			)
			return;
		CTextReader	TextReader	= new	CTextReader();
		//-------------------------------------------------------
		//	подсчитываю количество строк
		if	( ! TextReader.Open( FileName.Trim() , CAbc.CHARSET_DOS ) )
			return;
		__.Write( FileName );
		if	( ! TextReader.Read() )	{
			__.Print( " - пустой !");
			TextReader.Close();
			return;
		}
		else
			if	( TextReader.Value.Length > 100 ) {
				__.Print( " - уже обработан, пропускаю.");
				TextReader.Close();
				return;
			}
		int	Total	=	1;
		while	( TextReader.Read() )
			Total	++	;
		TextReader.Close();
		if	(  Total < 10 )	{
			__.Print( " - короткий, пропускаю.");
			return;
		}
		__.Write(" , строк " + Total.ToString() );
		//-------------------------------------------------------
		//	определяю строку, начиная с которой буду разбивать
		const	string	SEPARATOR	=	">> =";
		const	string	SEPARATOR2	=	"<<";
		int	Half	=	( Total >> 1 ) ;
		int	Before	=	0;
		int	Len	=	0;
		int	I	=	0;
		TextReader.Open( FileName.Trim() , CAbc.CHARSET_DOS );
		for	( I=1 ; I<Half ; I++ ) {
			if	( !TextReader.Read() )	{
				TextReader.Close();
				__.Print( " - ошибка чтения файла !");
				return;
			}
			if	(	( TextReader.Value.IndexOf( SEPARATOR ) > -1 )
				&&	( TextReader.Value.IndexOf( SEPARATOR2 ) > 0 )
				) {
				Before	=	I ;
				Len	=	TextReader.Value.Length;
			}
		}
		int	After	=	0;
		while	( TextReader.Read()  ) {
			I++;
			if	(	( TextReader.Value.IndexOf( SEPARATOR ) > -1 )
				&&	( TextReader.Value.IndexOf( SEPARATOR2 ) > 0 )
				) {
				After	=	I ;
				Len	=	TextReader.Value.Length;
				break;
			}
		}
		TextReader.Close();
		Half	=	0;
		if	(	( After != 0 )
			&&	( Before != 0 )
			)       {
			if	( ( Total - 2*(Before+2) ) < ( 2*(After+2)-Total ) )
				Half	=	Before;
			else
				Half	=	After;
		}
		else	{
			if	( Before == 0 )
				Half	=	After;
			if	( After == 0 )
				Half	=	Before;
			;
		}
		if	( Half == 0 ) {
			__.Print(" - разбиение невозможно.");
			return;
		}
		//-------------------------------------------------------
		//	записываю результирующий файл
		__.Write(" , c "+Half.ToString());
                string	TmpFileName	=	TargetFileName+".TMP";
		CTextWriter	TmpWriter	= new	CTextWriter();
		if	( ! TmpWriter.OpenForAppend( TmpFileName , CAbc.CHARSET_DOS ) ) {
			__.Print(" - ошибка записи файла " + TmpFileName);
			return;
		}
		TextReader.Open( FileName.Trim() , CAbc.CHARSET_DOS );
		for	( I=1 ; I < ( Half+3 ) ; I++ )
			if	( TextReader.Read() )
				if	( ! TmpWriter.Add( TextReader.Value , CAbc.CRLF ) ) {
					__.Print(" - ошибка записи файла " + TmpFileName );
					TmpWriter.Close();
					TextReader.Close();
					return;
				}
		TmpWriter.Close();
		CTextWriter	TargetWriter	= new	CTextWriter();
		if	( ! TargetWriter.OpenForAppend( TargetFileName , CAbc.CHARSET_DOS ) ) {
			__.Print(" - ошибка записи файла " + TargetFileName);
			return;
		}
		CTextReader	TmpReader	= new	CTextReader();
                TmpReader.Open( TmpFileName.Trim() , CAbc.CHARSET_DOS );
		bool	EndOfFile1	=	false;
		bool	EndOfFile2	=	false;
		do {
			if	( ! EndOfFile1 )
				if	( ! TmpReader.Read() )
					EndOfFile1	=	true;
			if	( ! EndOfFile2 )
				if	( ! TextReader.Read() )
					EndOfFile2	=	true;
			TargetWriter.Add(
				( ( EndOfFile1 ) ? __.Left( CAbc.EMPTY , Len ) : __.Left( TmpReader.Value , Len ) )
			,	"  "
			,	( ( EndOfFile2 ) ? __.Left( CAbc.EMPTY , Len ) : __.Left( TextReader.Value , Len ) )
			,	CAbc.CRLF
			);
		} while	(	( ! EndOfFile1 )
			||	( ! EndOfFile2 )
			);
		TargetWriter.Add( CAbc.FORM_FEED );
		TmpReader.Close();
		TargetWriter.Close();
		__.DeleteFile(TmpFileName);
		//-------------------------------------------------------
		__.Print(" . Готово.");
	}//FOLD01
	//--------------------------------------------------------------------------
	// ccda  ClientBank--Credit-reestr-Daily-All
	// ежеднеаная клиент-банковская кредитовка по всем
	static	void	CCDA( int DayDate ){//fold01
		int	BranchId	=	0;
		string	FileName	=	CAbc.EMPTY;
		CArray	BranchList	= new	CArray();
		Connection		= new	CConnection( ConnectionString ) ;
		if      ( ! Connection.IsOpen() ) {
			CCommon.Print("  Ошибка подключения к серверу !");
			return;
		}
		System.Console.Title="  Кpедитовки для К-Б  за " + CCommon.StrD( DayDate , 8 , 8 ) + "     |    "+ServerName+"."+DataBase	;
		CRecordSet	RecordSet	= new	CRecordSet( Connection ) ;
		if	( RecordSet.Open( "exec dbo.Mega_Common_GetBranchList " + DayDate.ToString() ) )
			while	( RecordSet.Read() )
				BranchList.Add(
					__.Left(	RecordSet["ID"]		,	20	)
				+	__.Left(	RecordSet["MailOut"]	,	224	)
				);
		RecordSet.Close();
		Connection.Close();
		CSc2Reports	Sc2Reports		= new	CSc2Reports();
		if	( Sc2Reports.Open( ConnectionString ) )
			foreach	( string BranchInfo in BranchList )  {
				BranchId	=	__.CInt( __.SubStr( BranchInfo , 0 , 19 ) );
				FileName	=	__.SubStr( BranchInfo , 20 , 243 ).Trim();
				if	( __.IsEmpty( FileName ) )
					continue;
				FileName	+=	"\\" + __.StrD( DayDate , 8 , 8 ).Replace("/","").Replace(".","") + ".CRD" ;
				__.Print("Кредитовку по филиалу " + BranchId.ToString() + " вывожу в файл " + FileName );
				Sc2Reports.CbCrdDoc( DayDate  , BranchId , FileName );
			}
		Sc2Reports.Close();
		__.Print("Все выписки построены.","Для продолжения нажмите Enter...");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// ceda  ClientBank--Extract-Daily-All
	// ежеднеаная клиент-банковская кредитовка по всем
	static	void	CEDA( int DayDate ) {//fold01
		int	BranchId	=	0;
		string	YuzhCable_Path	=	CAbc.EMPTY;
		string	MailOut		=	CAbc.EMPTY;
		CArray	BranchList	= new	CArray();
		Connection		= new	CConnection( ConnectionString ) ;
		if      ( ! Connection.IsOpen() ) {
			CCommon.Print("  Ошибка подключения к серверу !");
			return;
		}
		System.Console.Title="  Выписки для К-Б  за " + CCommon.StrD( DayDate , 8 , 8 ) + "      |   "+ServerName+"."+DataBase	;
		CRecordSet	RecordSet	= new	CRecordSet( Connection ) ;
		if	( RecordSet.Open( " select MailOut from dbo.SV_Branchs where Id= " + YUZHCABLE_ID.ToString() ) )
			if	( RecordSet.Read() )
				YuzhCable_Path	=	RecordSet[0].Trim();
		if	( RecordSet.Open( "exec dbo.Mega_Common_GetBranchList;2 " + DayDate.ToString() ) )
			while	( RecordSet.Read() )
				BranchList.Add(
					__.Left(	RecordSet["ID"]		,	20	)
				+	__.Left(	RecordSet["MailOut"]	,	224	)
				);
		RecordSet.Close();
		Connection.Close();
		if	( ! __.IsEmpty( YuzhCable_Path ) ) {
			string	OutFileName	=	YuzhCable_Path  + "\\" + __.StrD( DayDate , 8 , 8 ).Replace("/","").Replace(".","") ;
			CSc2Reports	Sc2Reports		= new	CSc2Reports();
			if	( Sc2Reports.Open( ConnectionString ) ) {
				__.Print("Спец.выписку по филиалу " + YUZHCABLE_ID.ToString() + " вывожу в " + YuzhCable_Path );
				Sc2Reports.YuzhCable (  DayDate , YUZHCABLE_ID , OutFileName );
			}
			Sc2Reports.Close();
		}
		CSc2Extract	Sc2Extract	= new	CSc2Extract();
		if	( Sc2Extract.Open( ConnectionString ) ) {
			if	( ! __.IsEmpty( YuzhCable_Path ) ) {
				Sc2Extract.Path		=	YuzhCable_Path;
				Sc2Extract.CbMode	=	true;
				Sc2Extract.CoolSum	=	false;
				Sc2Extract.OverMode	=	1;
				Sc2Extract.NeedPrintMsg	=	true;
				Sc2Extract.DateFrom	=	DayDate;
				Sc2Extract.DateInto	=	DayDate;
				Sc2Extract.BranchId	=	YUZHCABLE_ID ;
				Sc2Extract.GroupId	=	0;
				Sc2Extract.UserId	=	0;
				Sc2Extract.Build();
			}
			foreach	( string BranchInfo in BranchList )  {
				BranchId	=	__.CInt( __.SubStr( BranchInfo , 0 , 19 ) );
				MailOut		=	__.SubStr( BranchInfo , 20 , 243 ).Trim();
				if	(	( __.IsEmpty( MailOut ) )
					||	( BranchId == YUZHCABLE_ID )
					)
					continue;
				Sc2Extract.Path		=	MailOut;
				Sc2Extract.CbMode	=	true;
				Sc2Extract.CoolSum	=	false;
				Sc2Extract.OverMode	=	2;
				Sc2Extract.NeedPrintMsg	=	true;
				Sc2Extract.DateFrom	=	DayDate;
				Sc2Extract.DateInto	=	DayDate;
				Sc2Extract.BranchId	=	BranchId ;
				Sc2Extract.GroupId	=	0;
				Sc2Extract.UserId	=	0;
				Sc2Extract.Build();
			}
		}
		Sc2Extract.Close();
		__.Print("Все выписки построены.","Для продолжения нажмите Enter...");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// e2col  divide Extract into 2 Columns
	// разбить банковские выписки на 2 колонки
	static	void	E2COL( int DayDate ) {//fold01
		System.Console.Title=" Разбивка выписок на 2 колонки " ;
		string	Printer_ESC_Command;
		string	TargetFileName;
		string	DateStr		=	__.DtoC( DayDate );
		string[]FileNames	=	__.GetFileList( ScroogeOut + DateStr + "_U*.EXT" ) ;
		if	( FileNames == null )
			goto	CURRENCY_EXTRACT;
		if	( FileNames.Length == 0 )
			goto	CURRENCY_EXTRACT;
		TargetFileName	=	ScroogeOut + DateStr + ".EXT" ;
		if	( __.FileExists( TargetFileName ) )
			__.DeleteFile( TargetFileName );
		Printer_ESC_Command	=	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF;
		__.SaveText(  TargetFileName , Printer_ESC_Command , CAbc.CHARSET_DOS );
		foreach	( string FileName in FileNames )
			Extract2Columns( FileName  , TargetFileName );
		__.Print("Результат в " + TargetFileName);
	CURRENCY_EXTRACT:
		//  слияние валютных выписок в один файл
		__.Write("Слияние валютных выписок.");
		TargetFileName	=	ScroogeOut + DateStr + ".CXT" ;
		Printer_ESC_Command	=	__.Chr(27).ToString() + "P" + __.Chr(15).ToString() + CAbc.CRLF;
		__.SaveText( TargetFileName , Printer_ESC_Command , CAbc.CHARSET_DOS );
		FileNames	=	__.GetFileList( ScroogeOut + DateStr + "_U*.CXT" ) ;
		if	( FileNames != null )
			if	( FileNames.Length > 0 )
				foreach	( string FileName in FileNames ) {
					Append( TargetFileName , FileName );
					__.AppendText( TargetFileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
				}
		Printer_ESC_Command	=	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF;
		__.AppendText(  TargetFileName , Printer_ESC_Command , CAbc.CHARSET_DOS );
		Append( TargetFileName , ScroogeOut + DateStr + ".TRB" );
		__.AppendText( TargetFileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
		Append( TargetFileName , ScroogeOut + DateStr + ".TUR" );
		__.AppendText( TargetFileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
		Append( TargetFileName , ScroogeOut + DateStr + ".UZH" );
		__.AppendText( TargetFileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
		Printer_ESC_Command	=	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF;
		__.AppendText(  TargetFileName , Printer_ESC_Command , CAbc.CHARSET_DOS );
		__.Print(" Готово.");
		//--------------------------------------------------
		__.Print("Для продолжения нажмите Enter...");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// beda Bank--Extract-Daily-All
	// банковская выписка по всем
	static	void	BEDA( int DayDate ) {//fold01
		CSc2Reports	Sc2Reports	;
		CSc2Extract	Sc2Extract	;
		System.Console.Title=" Выписки банковские  за " + CCommon.StrD( DayDate , 8 , 8 ) + "   |   "+ServerName+"."+DataBase	;
		Sc2Reports		= new	CSc2Reports();
		if	( ! Sc2Reports.Open( ConnectionString ) )
			return;
		int	BranchId	=	Sc2Reports.Branch_Id;
		int	BranchKind	=	Sc2Reports.Branch_Kind;
		Sc2Reports.Close();
		if	( BranchKind > 0 ) {
		// Филиал
			Sc2Extract	= new	CSc2Extract();
			if	( Sc2Extract.Open( ConnectionString ) ) {
				Sc2Extract.Path		=	ScroogeOut;
				Sc2Extract.DateFrom	=	DayDate;
				Sc2Extract.DateInto	=	DayDate;
				Sc2Extract.CbMode	=	false;
				Sc2Extract.CoolSum	=	false;
				Sc2Extract.ApartFile	=	false;
				Sc2Extract.NeedPrintMsg	=	true;
				Sc2Extract.OverMode	=	2;
				Sc2Extract.BranchId	=	BranchId;
				Sc2Extract.GroupId	=	1;
				Sc2Extract.Build();
			}
			Sc2Extract.Close();
			__.Print("Выписки построены.","Для продолжения нажмите Enter...");
			CConsole.ClearKeyboard();
			CConsole.Flash();
			CConsole.ReadChar();
			return;
		}
		// Головной банк
		//
		// Выписки банковские
		int	UserId			=	0		;
		CArray	UahUserList		= new	CArray()	;
		CArray	CrncyUserList		= new	CArray()	;
		string	FileName		=	CAbc.EMPTY	;
		const	string	UAH_TAG		=	"UAH"		;
		const	string	ALL_CRNCY_TAG	=	"*"		;
		Connection		= new	CConnection( ConnectionString ) ;
		if      ( ! Connection.IsOpen() ) {
			CCommon.Print("  Ошибка подключения к серверу !");
			return;
		}
		CRecordSet	RecordSet	= new	CRecordSet( Connection ) ;
		if	( RecordSet.Open( "exec dbo.Mega_Common_GetBranchList;3 " + DayDate.ToString() + " , '" + UAH_TAG + "' " ) )
			while	( RecordSet.Read() )
				UahUserList.Add(
					__.Left(	RecordSet["UserId"]	,	20	)
				);
		if	( RecordSet.Open( "exec dbo.Mega_Common_GetBranchList;3 " + DayDate.ToString() + " , '" + ALL_CRNCY_TAG + "' " ) )
			while	( RecordSet.Read() )
				CrncyUserList.Add(
					__.Left(	RecordSet["UserId"]	,	20	)
				);
		RecordSet.Close();
		Connection.Close();
		Sc2Extract	= new	CSc2Extract();
		if	( Sc2Extract.Open( ConnectionString ) ) {
			Sc2Extract.Path		=	ScroogeOut;
			Sc2Extract.DateFrom	=	DayDate;
			Sc2Extract.DateInto	=	DayDate;
			Sc2Extract.CbMode	=	false;
			Sc2Extract.CoolSum	=	false;
			Sc2Extract.ApartFile	=	true;
			Sc2Extract.NeedPrintMsg	=	true;
			Sc2Extract.OverMode	=	2;
			// ----------------------------------------
			// по исполнителю 601
			Sc2Extract.SortMode	=	1;
			Sc2Extract.BranchId	=	0;
			Sc2Extract.GroupId	=	6;
			Sc2Extract.UserId	=	601;
			Sc2Extract.Build();
			// ---------------------------------------
			// по исполнителю 602
			Sc2Extract.SortMode	=	1;
			Sc2Extract.BranchId	=	0;
			Sc2Extract.GroupId	=	6;
			Sc2Extract.UserId	=	602;
			Sc2Extract.Build();
			// ----------------------------------------
			// по исполнителям текущих гривневых счетов в головном банке
			foreach	( string UserInfo in UahUserList )  {
				UserId			=	__.CInt( __.SubStr( UserInfo , 0 , 19 ) );
				Sc2Extract.CurrencyTag	=	UAH_TAG	;
				Sc2Extract.SortMode	=	0;
				Sc2Extract.BranchId	=	BranchId;
				Sc2Extract.GroupId	=	1;
				Sc2Extract.UserId	=	UserId;
				Sc2Extract.Build();
			}
			// по исполнителям текущих валютных счетов в головном банке
			foreach	( string UserInfo in CrncyUserList )  {
				UserId			=	__.CInt( __.SubStr( UserInfo , 0 , 19 ) );
				Sc2Extract.CurrencyTag	=	ALL_CRNCY_TAG	;
				Sc2Extract.SortMode	=	0		;
				Sc2Extract.BranchId	=	BranchId	;
				Sc2Extract.GroupId	=	1		;
				Sc2Extract.UserId	=	UserId		;
				Sc2Extract.Build();
			}
			// ----------------------------------------
			// по бывшему крымскому филиалу
			Sc2Extract.SortMode	=	0;
			Sc2Extract.BranchId	=	1000016;
			Sc2Extract.GroupId	=	0;
			Sc2Extract.UserId	=	0;
			Sc2Extract.AccCode	=	"260" ;
			Sc2Extract.Build();
			// -----------------------------------------
			// по Турбинке - валютная выписка
			Sc2Extract.CurrencyTag	=	"*";
			Sc2Extract.ApartFile	=	false;
			Sc2Extract.OverMode	=	1;
			Sc2Extract.UserId       =	0;
			Sc2Extract.GroupId      =	300;
			Sc2Extract.BranchId     =	0;
			Sc2Extract.AccCode	=	"" ;
			Sc2Extract.ClientCode   =	"JUR.1859";
			Sc2Extract.CoolSum	=	true;
			Sc2Extract.Build();
			FileName		=	ScroogeOut + __.DtoC( DayDate );
			if	( __.FileExists( FileName + ".CXT" ) ) {
				if	( __.FileExists( FileName + ".TUR" ) )
					__.DeleteFile( FileName + ".TUR" );
				__.MoveFile( FileName + ".CXT" , FileName + ".TUR" );
			}
			// ----------------------------------------
			// по Южкабелю - валютная выписка
			Sc2Extract.ApartFile	=	false;
			Sc2Extract.CurrencyTag	=	"*";
			Sc2Extract.AccCode	=	"2" ;
			Sc2Extract.OverMode	=	1;
			Sc2Extract.UserId       =	0;
			Sc2Extract.GroupId      =	0;
			Sc2Extract.BranchId     =	0;
			Sc2Extract.AccCode	=	"2" ;
			Sc2Extract.ClientCode   =	"JUR.7520";
			Sc2Extract.CoolSum	=	true;
			Sc2Extract.Build();
			FileName		=	ScroogeOut + __.DtoC( DayDate );
			if	( __.FileExists( FileName + ".CXT" ) ) {
				if	( __.FileExists( FileName + ".UZH" ) )
					__.DeleteFile( FileName + ".UZH" );
				__.MoveFile( FileName + ".CXT" , FileName + ".UZH" );
			}
		}
		Sc2Extract.Close();
		// Выписка по древовидным счетам турбинки
		FileName	=	ScroogeOut + __.DtoC( DayDate )+".TRB";
		__.Print( "Выписку по Турбинке вывожу в " + FileName );
		Sc2Reports		= new	CSc2Reports();
		if	( Sc2Reports.Open( ConnectionString ) )
			Sc2Reports.TreeExtract(  DayDate , DayDate , "JUR.1859" , FileName );
		Sc2Reports.Close();
		__.Print("Все выписки построены.","Для продолжения нажмите Enter...");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// загрузка #02-файлов в БД
	static	string	LoadFiles02( int FromDate , int ToDate , string[] FileNames ) {//fold01
		if	( FileNames == null )
			return	"Не указаны исходные файлы !";
		if	( FileNames.Length == 0 )
			return	"Не указаны исходные файлы !";
		foreach	( string FName in FileNames )
			if	( FName.Trim().Length == 0 )
				return	"Неправильное имя файла !";
		bool		SkipDelete	=	false		;
		string[]	SubList					;
		int		StrCount	=	0
		,		DayDate		=	0		;
		money		Active		=	0
		,		Passive		=	0
		,		Debit		=	0
		,		Credit		=	0		;
		string		Tmps		=	CAbc.EMPTY
		,		CmdText		=	CAbc.EMPTY
		,		BankCode	=	CAbc.EMPTY
		,		CleanName	=	CAbc.EMPTY	;
		CTextReader	TextReader	= new	CTextReader();
		Connection			= new	CConnection( ConnectionString ) ;
		CCommand	Command		= new	CCommand( Connection );
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		foreach	( string FName in FileNames ) {
			if	( FName == null )
				continue;
			CleanName	=	__.GetFileName( FName ).Trim();
			if	( CleanName.Length < 12 )
				continue;
			Tmps	=	CleanName.Substring( CleanName.Length - 9 , 3 );
			BankCode	=	(string) __.IsNull(  Command.GetScalar( " select code from SV_banks with (nolock) where SubString(eCode,2,3) ='" + Tmps + "' " ) , CAbc.EMPTY );
			if	( __.IsEmpty( BankCode ) ) {
				Command.Close();
				Connection.Close();
				CConsole.Clear();
				return	"Ошибка определения кода банка !";
			}
			if	( ! TextReader.Open( FName , CAbc.CHARSET_WINDOWS ) ) {
				TextReader.Close();
				continue;
			};
			// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			TextReader.Read();
			TextReader.Read();
			Tmps	=	TextReader.Value;
			SubList	=	Tmps.Split('=');
			if	( SubList == null )
				continue;
			else
				if	( SubList.Length < 10 )
					continue;
			// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			Tmps		=	SubList[2] + "00000000";
			DayDate		=	__.GetDate( Tmps.Substring(0,2) + "/"  + Tmps.Substring(2,2)  + "/"  + Tmps.Substring(4,4) );
			if	( FromDate != DayDate ) {
				Command.Close();
				Connection.Close();
				CConsole.Clear();
				return	"Несовпадение дат в отчетных файлах !";
			}
			Tmps		=	SubList[3] + "00000000";
			DayDate		=	__.GetDate( Tmps.Substring(0,2) + "/"  + Tmps.Substring(2,2)  + "/"  + Tmps.Substring(4,4) );
			if	( ToDate != DayDate ) {
				Command.Close();
				Connection.Close();
				CConsole.Clear();
				return	"Несовпадение дат в отчетных файлах !";
			}
			// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			if	( ! SkipDelete ) {
				SkipDelete	=	true;
				CmdText		=	"   exec  dbo.Mega_Report_PlainBalance;4 "
						+	"   @DayDate = " + ToDate.ToString()
						+	" ; exec  dbo.Mega_Report_Balance02;2  "
						+	"   @FromDate = " + FromDate.ToString()
						+	" , @ToDate = " + ToDate.ToString() ;
				if	( ! Command.Execute( CmdText ) ) {
					Command.Close();
					Connection.Close();
					TextReader.Close();
					return	"Ошибка выполнения команды на сервере !";
				}
			}
			// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			StrCount=0;
			while	( TextReader.Read() ) {
				CCommon.Write(" Загрузка "+CleanName + " . Строка  " + __.StrI( ++StrCount , 5 ) + "\r" );
				Tmps	=	TextReader.Value.Trim();
				if	( Tmps.Length < 12 )
					continue;
				Active=0;Passive=0;Debit=0;Credit=0;
				switch	( Tmps.Substring(0,2) ) {
					case	"10": {
						Active	=	__.CCur( Tmps.Substring(11) ) / 100 ;
						break;
					}
					case	"20": {
						Passive	=	__.CCur( Tmps.Substring(11) ) / 100 ;
						break;
					}
					case	"50": {
						Debit	=	__.CCur( Tmps.Substring(11) ) / 100 ;
						break;
					}
					case	"60": {
						Credit	=	__.CCur( Tmps.Substring(11) ) / 100 ;
						break;
					}
					default	: {
						break;
					}
				}
				// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
				CmdText		=	"   exec dbo.Mega_Report_PlainBalance;3 "
						+	"   @DayDate	= " + ToDate.ToString()
						+	" , @Bank	= " + BankCode
						+	" , @Topic	= " + Tmps.Substring(2,4)
						+	" , @Debit	= " + Debit.ToString().Replace(",",".")
						+	" , @Credit	= " + Credit.ToString().Replace(",",".")
						+	" , @Active	= " + Active.ToString().Replace(",",".")
						+	" , @Passive	= " + Passive.ToString().Replace(",",".")
						+	" ; exec  dbo.Mega_Report_Balance02;1   "
						+	"   @ParamCode	= '" + Tmps +"' "
						+	" , @FromDate	=  " + FromDate.ToString()
						+	" , @ToDate	=  " + ToDate.ToString() ;
				if	( ! Command.Execute( CmdText ) ) {
					Command.Close();
					Connection.Close();
					TextReader.Close();
					return	"Ошибка выполнения команды на сервере !";
				}
			}
			TextReader.Close();
			CCommon.Print("");
		}
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CmdText	=	" exec  dbo.Mega_Report_PlainBalance;5  @DayDate= " + ToDate.ToString() ;
		if	( ! Command.Execute( CmdText ) ) {
			Command.Close();
			Connection.Close();
			TextReader.Close();
			return	"Ошибка выполнения команды на сервере !";
		}
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		Command.Close();
		Connection.Close();
		CConsole.Clear();
		return	CAbc.EMPTY;
	}//FOLD01
	//--------------------------------------------------------------------------
	// простой ежедневный баланс
	static	void	BBDS( int DayDate  ) {//fold01
		CSc2Reports	Sc2Reports	= new	CSc2Reports();
		string		FileName	=	ScroogeOut + "\\" + __.StrD( DayDate , 8 , 8 ).Replace(".","").Replace("/","")+".bln";
		if	( Sc2Reports.Open( ConnectionString ) ) {
			__.Print("Вывожу отчет в файл "+FileName);
			Sc2Reports.Balance( FileName , DayDate , DayDate , ( Sc2Reports.Branch_Kind == 0 ) );
			__.AppendText( FileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
		}
		Sc2Reports.Close();
		__.Print("Готово. Для продолжения нажмите Enter.");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// сводный ежедневный баланс
	static	void	BBDC( int DayDate  ) {//fold01
		string[] FileNames	;
		FileNames	=	CCommon.OpenFilesBox(
					"Выберите файлы для загрузки"
				,	__.GetDirName( ScroogeOut )
				,	"#02-файлы (#02*.C*)|#02*.C*"
		);
		if	( FileNames == null )
			return;
		string	Msg	=	LoadFiles02( DayDate , DayDate , FileNames );
		if	( ! __.IsEmpty( Msg ) ) {
			CConsole.Clear();
			__.Print( CAbc.EMPTY , "Ошибка при загрузке #02 файлов " , CAbc.EMPTY , Msg , CAbc.EMPTY , "Для продолжения нажмите Enter.");
			CConsole.ClearKeyboard();
			CConsole.Flash();
			CConsole.ReadChar();
			return	;
		}
		CSc2Reports	Sc2Reports	= new	CSc2Reports();
		string		FileName	=	ScroogeOut + "\\" + __.StrD( DayDate , 8 , 8 ).Replace(".","").Replace("/","")+".svo";
		if	( Sc2Reports.Open( ConnectionString ) ) {
			__.Print("Вывожу отчет в файл  " + FileName);
			Sc2Reports.ConsolidatedBalance( FileName , DayDate , DayDate );
			__.AppendText( FileName , CAbc.FORM_FEED , CAbc.CHARSET_DOS );
		}
		Sc2Reports.Close();
		__.Print("Готово. Для продолжения нажмите Enter.");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
	}//FOLD01
	//--------------------------------------------------------------------------
	// запрос пользователю на ввод даты
	static	int	AskUserToEnterDate( string Prompt , int DefaultDate ) {//fold01
		CCommon.Write( Prompt );
		string	Answer		=	CCommon.Input().Trim() ;
		int	Result		=	0 ;
		if	( Answer.Length > 0 )
			Result	=	CCommon.GetDate( Answer );
		if	( Result == 0 )
			Result	=	DefaultDate;
		return	Result ;
	}//FOLD01
	//--------------------------------------------------------------------------
	// получение первого дня месяца
	public	static	int	GetFirstDayOfMonth( int DayDate ) {//fold01
		int	Month	=	CCommon.Month( DayDate ) ;
		int	Year	=	CCommon.Year( DayDate ) ;
		return	CCommon.GetDate( Year.ToString().Trim() + "." + Month.ToString().Trim() + ".01" );
	}//fold01
	//--------------------------------------------------------------------------
	// получение последнего дня месяца
	public	static	int	GetLastDayOfMonth( int DayDate ) {
		int	Day	=	1;
		int	Month	=	CCommon.Month( DayDate ) ;
		int	Year	=	CCommon.Year( DayDate ) ;
		if	( Month	< 12 )
			Month ++ ;
		else {
			Month = 1 ;
			Year ++ ;
		}
		int	FirstDayOfNextMonth	=	CCommon.GetDate( Year.ToString().Trim() + "." + Month.ToString().Trim() + "." + Day.ToString().Trim() );
		return	( FirstDayOfNextMonth - 1 ) ;
	}//FOLD01
	//--------------------------------------------------------------------------
	// ежемесячная банковская выписка по валютным счечам
	static	void	BEMC() {//fold01
		int	Choice	=	CConsole.GetMenuChoice(
					"  По всем валютным   "
				,	"По счетам Турбоатома "
				,	" По счетам Южкабеля  "
				,	" По счетам Газтепло  "
			) ;
		if	( Choice == 0 )
			return;
		int	DateFrom	=	AskUserToEnterDate("Введите начальную дату ( " + DATE_FROM_STR.Replace("/",".") + " ) :  " , DefaultDateFrom ) ;
		int	DateInto	=	AskUserToEnterDate("Введите конечную дату  ( " + DATE_INTO_STR.Replace("/",".") + " ) :  " , DefaultDateInto ) ;
		string	FileName	=	__.StrD( DateFrom , 8 , 8 ).Replace("/","").Substring(0,4)
					+	__.StrD( DateInto , 8 , 8 ).Replace("/","").Substring(0,4);
		System.Console.Title=" Валютные выписки  c " + CCommon.StrD( DateFrom , 8 , 8 ) + " по " + CCommon.StrD( DateInto , 8 , 8 ) + "  |   "+ServerName+"."+DataBase	;
		CSc2Extract Sc2Extract	= new	CSc2Extract();
		if	( Sc2Extract.Open( ConnectionString ) ) {
			string	NewExt		=	"";
			Sc2Extract.OverMode	=	1;
			Sc2Extract.CurrencyTag	=	"*";
			switch	( Choice ) {
				case	1: {	// По всем валютным
					Sc2Extract.GroupId	=	2;
					Sc2Extract.BranchId	=	1;
					NewExt			=	".ALL";
					__.SaveText(	ScroogeOut + FileName + ".CXT"
						,	__.Chr(27).ToString() + "P" + __.Chr(15).ToString() + CAbc.CRLF , CAbc.CHARSET_DOS );
					break;
				}
				case	2: {	// По счетам Турбоатома
					Sc2Extract.ClientCode	=	"JUR.1859";
					Sc2Extract.CoolSum	=	true;
					Sc2Extract.AllAmounts	=	true;
					NewExt			=	".TUR";
					__.SaveText(	ScroogeOut + FileName + ".CXT"
						,	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF , CAbc.CHARSET_DOS );
					break;
				}
				case	3: {	// По счетам Южкабеля
					Sc2Extract.ClientCode	=	"JUR.7520";
					Sc2Extract.AccCode	=	"2";
					NewExt			=	".UZH";
					__.SaveText(	ScroogeOut + FileName + ".CXT"
						,	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF , CAbc.CHARSET_DOS );
					break;
				}
				case	4: {	// По счетам Газтепло
					Sc2Extract.GroupId	=	299;
					Sc2Extract.CurrencyTag	=	"";
					__.SaveText(	ScroogeOut + FileName + ".CXT"
						,	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF , CAbc.CHARSET_DOS );
					NewExt			=	".GAZ";
					break;
				}
				default  : {
					Sc2Extract.Close();
					return;
				}
			}
			Sc2Extract.Path		=	ScroogeOut;
			Sc2Extract.DateFrom	=	DateFrom;
			Sc2Extract.DateInto	=	DateInto;
			Sc2Extract.NeedPrintMsg	=	true;
			Sc2Extract.ApartFile	=	false;
			Sc2Extract.CbMode	=	false;
			Sc2Extract.Build();
			__.AppendText(	ScroogeOut + FileName + ".CXT"
					,	__.Chr(18).ToString() + __.Chr(27).ToString() + "M" + CAbc.CRLF , CAbc.CHARSET_DOS );
			if	( __.FileExists( ScroogeOut + FileName + NewExt ) )
				if	( ! __.DeleteFile( ScroogeOut + FileName + NewExt ) )
				__.Print( "Ошибка удаления старого файла  " + ScroogeOut + FileName + NewExt );
			if	( ! __.MoveFile( ScroogeOut + FileName + ".CXT" , ScroogeOut + FileName + NewExt ) )
				__.Print( "Ошибка переименования файла  " + ScroogeOut + FileName + ".CXT" );
			else
				__.Print( "Выписки находятся в файле  " + ScroogeOut + FileName + NewExt );
		}
		Sc2Extract.Close();
		__.Print("Для продолжения нажмите Enter...");
		CConsole.ClearKeyboard();
		CConsole.Flash();
		CConsole.ReadChar();
		return;
	}//FOLD01
}