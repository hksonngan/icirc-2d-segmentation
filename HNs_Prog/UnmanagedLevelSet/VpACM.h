#pragma once
#include "VpGlobalDefine.h"

#define		DEFAULT_ITERATION	10000
#define		DEFAULT_DELTAT		.1f
#define		DEFAULT_H			1

#define		IGNORED				0
#define		BACKGRND			1
#define		OBJECT				2

typedef float	acmtype;

template<typename T>
class CVpACM
{
public:
	enum eDataType
	{
		Byte,
		Short,
	};

	enum eInit
	{
		BIGONE,
		SMALLMANY,
		RANDOM,
	//	PRECV, 
		MASK, 
		NONE
	};

protected:
	int m_nWidth;
	int m_nHeight;
	int m_nDepth;
	int m_nWH;
	T**	m_ppImg;
	BYTE** m_ppMask;
	int	m_nIter;
	float m_fDeltaT;
	int	m_nH[3];
	int m_nInitEdgeLength;  // length of cube at small-many initialization

	eInit	m_eInit;
	acmtype** m_ppPhi;
	acmtype** m_ppEdgeIndicator;
	acmtype m_fPhiValue;

	void InitPhiSmallMany();
	void InitPhiRandom();
	void InitPhiPreCV();	
	void InitPhiMask();	

	float GetRandom(float fmin, float fmax);

public:
	CVpACM();	
	virtual ~CVpACM();

	BOOL InitPhi(eInit e, acmtype val = 1.0f);
inline	void Load(int w, int h, int d, T** ppData)	{ m_nWidth = w; m_nHeight = h; m_nDepth = d; m_nWH = w*h; m_ppImg = ppData;}	
inline	BOOL IsEmpty()								{ if (m_ppImg == NULL) return TRUE;	return FALSE; }
inline	void SetMask(BYTE** ppMask)	{m_ppMask = ppMask;}
inline  float** GetPhi() {return m_ppPhi;}
inline  void SetPhi(float** ppPhi) {m_ppPhi = ppPhi;}
inline	void SetInitEdgeLength(int n)	{m_nInitEdgeLength = n;}
inline	void SetIter(int n)	{m_nIter = n;}
inline	void SetDeltaT(float f)	{m_fDeltaT = f;}

	virtual BOOL Do(BYTE** ppOut);
	virtual void Release();
};

template class CVpACM<BYTE>;
template class CVpACM<short>;


class CVpACM2D
{

protected:
	// member variable
	int			m_nWidth;
	int			m_nHeight;

	union
	{
		BYTE*		m_pbytData; 
		voltype*	m_pvolData; 
	};

public:
	CVpACM2D();
	virtual ~CVpACM2D();

	// member method
	void Init(int w, int h, BYTE* pData)
	{
		m_nWidth = w;
		m_nHeight = h;
		m_pbytData =  pData;
	}	
	void Init(int w, int h, voltype* pData)
	{
		m_nWidth = w;
		m_nHeight = h;
		m_pvolData =  pData;
	}	
};


class CVpACM3D
{

protected:
	// member variable
	int			m_nWidth;
	int			m_nHeight;
	int			m_nDepth;

	union
	{
		BYTE**		m_ppbytData;
		voltype**	m_ppvolData;
	};

public:
	CVpACM3D();
	virtual ~CVpACM3D();

	// member method
/*
	template <typename T>
	void Init(int w, int h, int d, T** ppData)
	{
		m_nWidth = w;
		m_nHeight = h;
		m_nDepth = d;

		m_ppbytData = (T**) ppData;
	}	
*/
};