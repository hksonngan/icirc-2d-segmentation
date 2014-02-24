#pragma once

//========= Include ===========//
#include	<math.h>
#include	<algorithm>
#include	<vector>
#include	<list>
#include	<map>
#include	<set>
#include	<stack>

//========= Define Mode ===========//
#define			_UF3D_APPMODE_DEBUG_
#define			_UF3D_APPMODE_MFC_				//_UF3D_APPMODE_WIN32_
#define			_UF3D_GEODATA_FLOAT_			//_UF3D_GEODATA_DOUBLE_
#define			_UF3D_IDXDATA_SHORT_			//_UF3D_IDXDATA_INT_


//========= Macro ===========//
#define		LOCAL						static
#define		GLOBAL						extern
#if defined		_UF3D_APPMODE_DEBUG_
	#define		MsgDebug(x)		AfxMessageBox(_T(x))
#else
	#define		MsgDebug(x)
#endif

#define		NOTAVAILABLE		AfxMessageBox(_T("Not Available...!!!"))

#define IS_KEYDOWN(virKey) ((GetKeyState(virKey) & 0xFF00) ? true : false)

#define		SAFEFUNC(x,y) if(x) y

#define		GETIDX_X(x)		(3*x)
#define		GETIDX_Y(y)		(3*y+1)
#define		GETIDX_Z(z)		(3*z+2)
#define		GETIDX_U(u)		(2*u)
#define		GETIDX_V(v)		(2*v+1)
#define		UF_INFINITY		1e20
#define		UF_EPSILON		1e-8
#define		FLTEPSILON		0.0000001f

#define		TIMERID_IDLE				1024
#define		TIMERID_LOADHEADTEMPLATE	3000
#define		TIMERID_CREATEFACEEND		3010
#define		TIMERID_LOADHAIR			3020
#define		TIMERID_EXPORTHEAD			3030
#define		TIMERID_NOTFACE				3040
#define		TIMERID_NOIMAGE				3050

#define		INTERVAL_IDLE				1
#define		INTERVAL_LOADHEADTEMPLATE	2000
#define		INTERVAL_CREATEFACEEND		3000
#define		INTERVAL_LOADHAIR			5000
#define		INTERVAL_EXPORTHEAD			8000
#define		INTERVAL_NOTFACE			3000
#define		INTERVAL_NOIMAGE			3000

#define		DEFAULT_FPS			30
#define		INF					1E20

//========= Macro path ===========//



//========= Type Define ===========//
typedef		unsigned int		uint;
typedef		unsigned short		ushort;
typedef		void				(*PFNRENDERPROC)(void);
#if defined		_UF3D_GEODATA_FLOAT_
	typedef			float				vtype;
	#define			GL_VTYPE	GL_FLOAT
#else
	typedef			double				vtype;
	#define			GL_VTYPE	GL_DOUBLE
#endif
typedef		double				mathtype;

typedef		vtype*				VertexArrayPtr;
typedef		int*				IndexArrayPtr;
typedef		vtype*				TextureArrayPtr;
typedef		vtype*				NormalArrayPtr;
typedef		unsigned short		uint16;

typedef		short				voltype;
typedef		float				lstype;

#if defined		_UF3D_IDXDATA_SHORT_
	typedef		unsigned short		tdIdxType;
	#define		GL_IDXTYPE			GL_UNSIGNED_SHORT	
#else
	typedef		int					tdIdxType;
	#define		GL_IDXTYPE			GL_UNSIGNED_INT	
#endif
typedef		tdIdxType*			tdIdxTypePtr;

typedef		std::vector<int>			IndexVector;
typedef		std::vector<int>			IndexVertex;

typedef		ULONGLONG			UFCombinedID;
typedef		unsigned int		UFID;


#if defined		_UNICODE
	#define		strT(x)		_T(x)
	typedef		wchar_t		cchar;
#else
	#define		strT(x)		x
	typedef		char		cchar;
#endif


template<typename T>
class TPoint4D
{
public:
	T x;
	T y;
	T z;
	T w;
};

template<typename T>
class TPoint3D
{
public:
	T x;
	T y;
	T z;
};


template<typename T>
class TPoint2D
{
public:
	T x;
	T y;
};

//========= Constant Global Variable ===========//
const	int		gconst_iCountofMaxLight		=	8;
const	int		gconst_iCountofMaxMaterial	=	8;
const	double	gconst_HALFPI				=	1.57079632679489661923;
const	double	gconst_dPI					=	3.14159265358979323846;
const	double	gconst_d2PI					=	6.28318530717958647692;
const	double	gconst_dPIover180			=	0.01745329251994329576;
const	double	gconst_d1Radian				=	57.2957795130823208768;

enum EnType_Axis
{
	gconst_iAxisX	=	0,
	gconst_iAxisY,
	gconst_iAxisZ
};

enum UFStatus
{
	UFSTATUS_OK,
	UFSTATUS_WARING,
	UFSTATUS_ERROR,
};

enum eUFApplcationType
{	
	UF_APP_MFC_TOOL,
	UF_APP_OCX_FACE,
	UF_APP_END,
};

enum eBpp
{
	BPP_8U,
	BPP_16S,
};

//============= inline function ================//
inline wchar_t* GetWChar(CString& str)
{
	USES_CONVERSION;
	return (T2W(str.GetBuffer()));
}

inline void UseEnd(CString& str)
{
	str.ReleaseBuffer();
}

/*
inline char* Wchar2Char(CStringW& str)
{	
int nlen = (int)wcslen(str) + 1;
char *pmbbuf = new char[nlen];

USES_CONVERSION;
wchar_t* pwstr = T2W(str.GetBuffer());

wcstombs( pmbbuf, str, nlen );

str.ReleaseBuffer();
return pmbbuf;
}
*/

inline wchar_t* Char2Wchar(char* str)
{	_ASSERT(str);
	int nlen = (int)strlen(str) + 1;
	wchar_t *pwc = new wchar_t[nlen];
	mbstowcs(pwc, str, nlen);
	return pwc;
}

inline char* Wchar2Char(wchar_t* str)
{	
	_ASSERT(str);
	int nlen = (int)wcslen(str) + 1;
	char *pmbbuf = new char[nlen];
	wcstombs( pmbbuf, str, nlen );
	return pmbbuf;
}



//========= template inline function ===========//

enum eSDBEntityType;

template<class T1, class T2>
inline UFCombinedID MakeCombinedID(T1 eType, T2 selfID)
{
	return (UFCombinedID)((((UFCombinedID)eType) << 32) | ((UFCombinedID)(selfID)));
}

inline UFID ExtractCombinedFirst(UFCombinedID combineID)
{
	UFID selfID = (UFID)(combineID);
	return selfID;
}	

inline UFID ExtractCombinedSecond(UFCombinedID combineID)
{
	UFID eType = (UFID)(combineID >> 32);
	return eType;
}


//========= template class ===========//
template<class T>
class	STRCTTBL
{
public:
	const	cchar*	strExt;
	T*	pMdl;

};


class	STRINTTBL
{
public:
	const	wchar_t* strExt;
	int				nIdx;
};
template<typename T>	
inline void SafeFree(T*& x)
{
	if (x)
	{
		free(x);
		x = NULL;
	}
}

template<typename T>	
inline void SafeNew(T*& x)
{
	if (x)
		delete(x);

	x = new T;
}


template<typename T>	
inline BOOL SafeNews(T*& x, const int a)
{
	if (x)
		delete[](x);

	x = new T[a];
	memset(x, 0, sizeof(T)*a);

	if (x==NULL) return FALSE;
	return TRUE;
}

template<typename T>
inline BOOL SafeNew2D(T**& x, const int wh, const int d)
{
	x = new T*[d];
	if (!x) return FALSE;

	for(int i = 0; i < d; i++)
	{
		x[i] = new T[wh];
		memset(x[i], 0, sizeof(T)*wh);
	}
	if (!x[d-1]) return FALSE;

	return TRUE;
}

template<typename T>
inline void SafeDelete2D(T**& x, const int wh, const int d)
{
	for(int i = 0; i < d; i++)
	{
		SafeDeletes(x[i]);
	}
	delete[] x;
	x = NULL;
}


template<typename T>	
inline void SafeDelete(T*& x)
{
	if (x)
		delete(x);

	x = NULL;
}


template<typename T>	
inline void SafeDeletes(T*& x)
{
	if (x)
		delete[](x);

	x = NULL;
}

template<typename T>	
inline void SafeRelease(T*& x)
{
	if (x)
	{
		x->Release();
		delete (x);
		x = NULL;
	}
}


template<typename T>
inline void VirFree(T*& x, const int a)
{
	if (x)
	{
		VirtualFree(x, a*sizeof(T), MEM_DECOMMIT);
		VirtualFree(x, 0, MEM_RELEASE); 
	}
	x = NULL;
}


template<typename T>
inline BOOL VirAlloc(T*& x, const int a)
{
	VirFree<T>(x, a);
	x = (T*)::VirtualAlloc(NULL, a*sizeof(T), MEM_COMMIT | MEM_RESERVE | MEM_TOP_DOWN, PAGE_READWRITE);
	memset(x, 0, sizeof(T)*a);
	if (x) return TRUE;
	return FALSE;
}

template<typename T>
inline void VirFree2D(T**& x, const int wh, const int d)
{
	if (x)
	{
		for(int i = 0; i < d; i++)
		{
			VirtualFree(x[i], wh*sizeof(T), MEM_DECOMMIT);
			VirtualFree(x[i], 0, MEM_RELEASE); 
			x[i] = NULL;
		} 
		VirtualFree(x, d*sizeof(T*), MEM_DECOMMIT);
		VirtualFree(x, 0, MEM_RELEASE); 
		x = NULL;
	}
}

template<typename T>
inline BOOL VirAlloc2D(T**& x, const int wh, const int d)
{
	VirFree2D<T>(x, wh, d);

	x = (T**)::VirtualAlloc(NULL, d*sizeof(T*), MEM_COMMIT | MEM_RESERVE | MEM_TOP_DOWN, PAGE_READWRITE);
	if (!x) return FALSE;

	for(int i = 0; i < d; i++)
	{
		x[i] = (T*)::VirtualAlloc(NULL, wh*sizeof(T), MEM_COMMIT | MEM_RESERVE | MEM_TOP_DOWN, PAGE_READWRITE);
		memset(x[i], 0, sizeof(T)*wh);
	}
	if (!x[d-1]) return FALSE;
	
	return TRUE;
}

template<typename T>	
inline mathtype Rad(T x)
{
	return (x*gconst_dPIover180);
}

template<typename T>	
inline mathtype Degree(T x)
{
	return (x*gconst_d1Radian);
}

template<typename T>	
inline T Rnd(int n, T x)
{
	double	nPow = (double)pow((double)10, (double)n);
	int		i = static_cast<int>(x * nPow);
	double	d = (i / nPow);

	return (T) d;
}

template<typename T>	
inline BOOL Compare(T x, T tmin, T tmax)
{
	if ((x > tmin) && (x < tmax)) return TRUE;
	return FALSE;
}

template<typename T>	
inline BOOL CompareEq(T x, T tmin, T tmax)
{
	if ((x >= tmin) && (x <= tmax)) return TRUE;
	return FALSE;
}

template<class T>
inline void SWAP(T &a, T &b)
{T dum=a; a=b; b=dum;}


template<typename T>	
inline T Max(T a, T b)
{
	if (a >= b) return a;
	return b;
}

template<typename T>	
inline T Min(T a, T b)
{
	if (a <= b) return a;
	return b;
}

template<typename T>	
inline T Abs(T a)
{
	if (a >= 0) return a;
	return (-a);
}

template <typename T>
inline T Square(const T &x) { return x*x; };

inline BOOL GetIdx1DTo3D(int idx, int w, int h, int d, int* x, int* y, int* z)
{
	int wh = w*h;
	*z = idx / wh;
	int xy = idx - (*z)*wh;
	*y = xy / w;
	*x = xy - (*y)*w;

	return TRUE;
}
inline BOOL GetIdx3DTo1D(int x, int y, int z, int w, int h, int d, int* idx)
{
	*idx = x + y*w + z*w*h;
	return TRUE;
}

inline BOOL GetIdx1DTo2D(int idx, int w, int h, int d, int* xy, int* z)
{
	int wh = w*h;
	*z = idx / wh;
	*xy = idx - (*z)*wh;

	return TRUE;
}
inline BOOL GetIdx2DTo1D(int xy, int z, int w, int h, int d, int* idx)
{
	*idx = xy + z*w*h;
	return TRUE;
}
inline BOOL GetIdx3DTo2D(int x, int y, int z, int w, int* idxxy, int* idxz)
{
	*idxz = z;
	*idxxy = x+y*w;

	return TRUE;
}

inline BOOL GetIdx2DTo3D(int idxxy, int idxz, int w, int* x, int* y, int* z)
{
	*z = idxz;
	*y = idxxy / w;
	*x = idxxy - (*y)*w;

	return TRUE;
}




//template class __declspec(dllimport) CStringT<TCHAR, StrTraitMFC<TCHAR, ChTraitsCRT<TCHAR> > >;
//template class __declspec(dllimport) CSimpleStringT<TCHAR>;

// enum EnType_MouseControl
// {
// 	eMouseCtrl_HORZ_VERT = 0,
// 	eMouseCtrl_HORZ_ONLY,
// 	eMouseCtrl_VERZ_ONLY,
// 	eMouseCtrl_VERZ_30,
// 	eMouseCtrl_END,
// };


#define		DEFINE_MAPFUNC_RET(id,key,ret,obj,param)	\
	typedef	ret (obj::*pfn)param;						\
	map<key, pfn>	m_mapFunc##id;						\


#define		DEFINE_MAPFUNC(id,key,obj,param)			\
	typedef	void (obj::*pfn)param;						\
	map<key, pfn>	m_mapFunc##id;						\


#define		INIT_MAPFUNC(id,key,obj,func)				\
	m_mapFunc##id[key]=&obj::func;						\


#define		CHECK_ENABLECALL(id)						\
	if (m_mapFunc##id.size() == 0) return;				\


#define		CALL_MAPFUNC(id,key,param)					\
	(this->*m_mapFunc##id[key])param;					\



#define		DEF_PROPERTY(t, x)							\
	private:											\
	t		x;											\
	public:												\
	t		get_##x()		{ return x; }				\
	void	set_##x(t val)	{ x = val;  }				\


#define		DEF_PROPERTY_REF(t, x)						\
	private:											\
	t		x;											\
	public:												\
	t&		get_##x()		{ return x; }				\
	void	set_##x(t& val)	{ x = val;  }				\


#define		DEF_PROPERTY_REF_VEC(t, x)						\
	private:												\
	vector<t>	x;											\
	public:													\
	vector<t>&	get_##x()				{ return x; }		\
	void		set_##x(vector<t>& val)	{ x = val;  }		\
	t&			get_##x(int n)			{ return x[n]; }	\
	void		set_##x(int n, t& val)	{ x[n] = val;  }	\


#define		DEF_PROPERTY_INTER(t, node, x)							\
	t		get_##x(){												\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	return pNode->get_##x();}										\
	void	set_##x(t val){											\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	pNode->set_##x(val);}											\


#define		DEF_PROPERTY_INTER_REF(t, node, x)						\
	t&		get_##x(){												\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	return pNode->get_##x();}										\
	void	set_##x(t& val){										\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	pNode->set_##x(val);}											\


#define		DEF_PROPERTY_INTER_REF_VEC(t, node, x)					\
	vector<t>&		get_##x(){										\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	return pNode->get_##x();}										\
	void	set_##x(vector<t>& val){								\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	pNode->set_##x(val);}											\
	t&		get_##x(int n){											\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	return pNode->get_##x(n);}										\
	void	set_##x(int n, t& val){									\
	node* pNode = dynamic_cast<node*>(UFSCENE->toSDBObject(m_ID));	\
	pNode->set_##x(n, val);}	


#define		GET_PROPERTY(obj,x)			obj.get_##x();
#define		SET_PROPERTY(obj,x,val)		obj.set_##x(val);

#define		GET_PROPERTY_N(obj,x,n)			obj.get_##x(n);
#define		SET_PROPERTY_N(obj,x,n,val)		obj.set_##x(n,val);

#define		GET_PROPERTY_PNT(obj,x)			obj->get_##x();
#define		SET_PROPERTY_PNT(obj,x,val)		obj->set_##x(val);

#define		GET_PROPERTY_PNT_N(obj,x,n)			obj->get_##x(n);
#define		SET_PROPERTY_PNT_N(obj,x,n,val)		obj->set_##x(n,val);

#define		LOOP1_START(i,w)							for(int i=0;i<w;i++){
#define		LOOP1_END			}
#define		LOOP2_START(i,j,w,h)						for(int j=0;j<h;j++){	for(int i=0;i<w;i++)	{
#define		LOOP2_START_IDX1(i,j,w,h,idx)				for(int j=0;j<h;j++){	for(int i=0;i<w;i++)	{	int idx=i+j*w;
#define		LOOP2_START_IDX3(i,j,w,h,idx)				for(int j=0;j<h;j++){	for(int i=0;i<w;i++)	{	int idx=(i+j*w)*3;
#define		LOOP2_START_OFFSET(i,j,w,h,n)				for(int j=n;j<h-n-1;j++){	for(int i=n;i<w-n-1;i++)	{
#define		LOOP2_START_IDX1_OFFSET(i,j,w,h,idx,n)		for(int j=n;j<h-n-1;j++){	for(int i=n;i<w-n-1;i++)	{	int idx=i+j*w;

#define		LOOP2_NEIGHBOR(m,n)							for(int n=-1;n<=1;n++){	for(int m=-1;m<=1;m++)	{
#define		LOOP2_NEIGHBORDEF							for(int n=-1;n<=1;n++){	for(int m=-1;m<=1;m++)	{

#define		LOOP2_END			}}

#define		LOOP2_STARTDEF							for(int j=0;j<h;j++){	for(int i=0;i<w;i++)	{	int idx=i+j*w;

#define		CREATEIMAGE1(w,h)						IplImage* pImg = cvCreateImage(cvSize(w,h),IPL_DEPTH_8U,1); cvZero(pImg);
#define		CREATEIMAGE3(w,h)						IplImage* pImg = cvCreateImage(cvSize(w,h),IPL_DEPTH_8U,3); cvZero(pImg);

#define		CREATEIMAGE1Ex(w,h,pImg)				IplImage* pImg = cvCreateImage(cvSize(w,h),IPL_DEPTH_8U,1); cvZero(pImg);
#define		CREATEIMAGE3Ex(w,h,pImg)				IplImage* pImg = cvCreateImage(cvSize(w,h),IPL_DEPTH_8U,1); cvZero(pImg);


#define		GET_VALUEPP(pp,w,x,y,z)					pp[z][y+x*w]