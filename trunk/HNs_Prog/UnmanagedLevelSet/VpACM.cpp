#include "stdafx.h"
#include "VpACM.h"


//////////////////////////////////////////////////////////////////////
// Active Contour Model
//////////////////////////////////////////////////////////////////////
template<typename T>
CVpACM<T>::CVpACM()
: m_nWidth(0), m_nHeight(0), m_nDepth(0), m_nWH(0), m_ppImg(NULL), m_ppMask(NULL), m_nIter(DEFAULT_ITERATION), m_fDeltaT(DEFAULT_DELTAT), m_ppPhi(NULL), m_nInitEdgeLength(3), m_fPhiValue(1.0f)
{
	m_nH[0] = DEFAULT_H;
	m_nH[1] = DEFAULT_H;
	m_nH[2] = DEFAULT_H;

	m_ppEdgeIndicator = NULL;
}


template<typename T>
CVpACM<T>::~CVpACM()
{

}


template<typename T>
BOOL CVpACM<T>::Do(BYTE** ppOut)
{
	return FALSE;
}


template<typename T>
void CVpACM<T>::Release()
{
	VirFree2D(m_ppPhi, m_nWH, m_nDepth);
	VirFree2D(m_ppEdgeIndicator, m_nWH, m_nDepth);

	m_nWidth = 0;
	m_nHeight = 0;
	m_nDepth = 0;
	m_nWH = 0;
}


template<typename T> 
float CVpACM<T>::GetRandom(float fmin, float fmax)
{
	int n = 101;
	int num = rand()%n; // 0 ~ 100
	float alpha = (float)num / (float)(n-1);
	float weight = alpha*fmax + (1.0f-alpha)*fmin;

	if (weight < fmin) weight = fmin;
	if (weight > fmax) weight = fmax;

	return weight;
}


template<typename T> 
void CVpACM<T>::InitPhiSmallMany()
{
	srand((unsigned)time(NULL));
	int n = 1001;
	int offset = m_nInitEdgeLength<<1;

	// initialize
	for (int xy = 0 ; xy < m_nWH ; xy++)	
	{			
		m_ppPhi[0][xy] = 0;//GetRandom(.0f, 1.0f);
	}
	for (int z = 1 ; z < m_nDepth ; z++)	
	{
		memcpy(m_ppPhi[z], m_ppPhi[z-1], sizeof(acmtype)*m_nWH);
	}
	
	if (!m_ppMask)
	{
		for (int z = 0 ; z < m_nDepth-offset ; z+=offset)	
		{
			for (int y = 0 ; y < m_nHeight-offset ; y+=offset)	
			{
				for (int x = 0 ; x < m_nWidth-offset ; x+=offset)	
				{					
					int idxxy = x + y*m_nWidth;

					for (int dz = 0 ; dz < m_nInitEdgeLength ; dz++)
					{
						for (int dy = 0 ; dy < m_nInitEdgeLength ; dy++)
						{
							for (int dx = 0 ; dx < m_nInitEdgeLength ; dx++)
							{
								int idxz = z + dz;
								int idxxy2 = idxxy + dx + dy*m_nWidth;
								m_ppPhi[idxz][idxxy2] = GetRandom(-m_fPhiValue, m_fPhiValue);
							}
						}
					}
				}
			}
		}
	}
	else
	{
		for (int z = 0 ; z < m_nDepth-offset ; z+=offset)	
		{
			for (int y = 0 ; y < m_nHeight-offset ; y+=offset)	
			{
				for (int x = 0 ; x < m_nWidth-offset ; x+=offset)	
				{					
					int idxxy = x + y*m_nWidth;

					for (int dz = 0 ; dz < m_nInitEdgeLength ; dz++)
					{
						for (int dy = 0 ; dy < m_nInitEdgeLength ; dy++)
						{
							for (int dx = 0 ; dx < m_nInitEdgeLength ; dx++)
							{
								int idxz = z + dz;
								int idxxy2 = idxxy + dx + dy*m_nWidth;
								if (m_ppMask[idxz][idxxy2] > 0)	m_ppPhi[idxz][idxxy2] = GetRandom(-m_fPhiValue, m_fPhiValue);
							}
						}
					}
				}
			}
		}
	}
}


template<typename T> 
void CVpACM<T>::InitPhiRandom()
{
	srand((unsigned)time(NULL));
	int n = 1001;
	for (int z = 0 ; z < m_nDepth ; z++)	
	{
		for (int y = 0 ; y < m_nHeight ; y++)	
		{
			for (int x = 0 ; x < m_nWidth ; x++)	
			{					
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[z][idxxy] == 0))
				{
					m_ppPhi[z][idxxy] = 0;
				}
				else
				{
					int num = rand()%n; // 0 ~ 1000
					float fnum = (num/500.0f) - 1.0f;
					m_ppPhi[z][idxxy] = fnum;
				}				
			}
		}
	}
}


template<typename T> 
void CVpACM<T>::InitPhiPreCV()
{
	// TODO :
}


template<typename T> 
void CVpACM<T>::InitPhiMask()
{
	if (!m_ppMask) return;

	for (int z = 0 ; z < m_nDepth ; z++)	
	{
		for (int y = 0 ; y < m_nHeight ; y++)	
		{
			for (int x = 0 ; x < m_nWidth ; x++)	
			{					
				int idxxy = x + y*m_nWidth;
				if (m_ppMask[z][idxxy] > 0)
				{
					m_ppPhi[z][idxxy] = -m_fPhiValue;
				}
				else
				{
					m_ppPhi[z][idxxy] = m_fPhiValue;
				}				
			}
		}
	}
}


template<typename T> 
BOOL CVpACM<T>::InitPhi(eInit e, acmtype val)
{
	if (!VirAlloc2D(m_ppPhi, m_nWH, m_nDepth)) return FALSE;
	m_fPhiValue = val;

	switch(e)
	{
	case BIGONE:
		break;

	case SMALLMANY:
		InitPhiSmallMany();
		break;

	case RANDOM:
		InitPhiRandom();
		break;

// 	case PRECV:
// 		InitPhiPreCV();
// 		break;

	case MASK:
		InitPhiMask();
		break;

	default:
		break;
	}

	m_eInit = e;

	return TRUE;
}


//////////////////////////////////////////////////////////////////////
// Active Contour Model 2D
//////////////////////////////////////////////////////////////////////

CVpACM2D::CVpACM2D()
: m_nWidth (0), m_nHeight(0), m_pbytData(NULL)
{

}

CVpACM2D::~CVpACM2D()
{

}




//////////////////////////////////////////////////////////////////////
// Active Contour Model 3D
//////////////////////////////////////////////////////////////////////

CVpACM3D::CVpACM3D()
: m_nWidth (0), m_nHeight(0), m_nDepth(0), m_ppbytData(NULL)
{

}

CVpACM3D::~CVpACM3D()
{

}


