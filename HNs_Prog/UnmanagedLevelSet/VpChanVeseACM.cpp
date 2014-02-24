#include "stdafx.h"
#include "VpChanVeseACM.h"

#include <math.h>
#include <float.h>
#include <algorithm>
#include <vector>
#include <list>
#include <map>
#include <set>
#include <stack>

#define OMP_PARALLEL

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////
template<typename T>
CVpChanVeseACM<T>::CVpChanVeseACM()
: m_eFidelity(L2), 	m_fMu(DEFAULT_MU), m_fLamda(DEFAULT_LAMDA), m_fEdgeTermWeight(DEFAULT_EDGETERMWEIGHT)
{
	m_fC[0] = .0f;
	m_fC[1] = .0f;
}


template<typename T>
CVpChanVeseACM<T>::~CVpChanVeseACM()
{

}


template<typename T> 
float CVpChanVeseACM<T>::CalcEnergy()
{
	float energy = .0f;

	int numC[2] = {0, 0};
	float sumC[2] = {.0f, .0f};
	
	if (m_nDepth > 1)
	{

#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int z = 0 ; z < m_nDepth ; z++)	
		{
			for (int y = 0 ; y < m_nHeight ; y++)	
			{
				for (int x = 0 ; x < m_nWidth ; x++)	
				{
					int idxxy = x + y*m_nWidth;
					if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

					if (m_ppPhi[z][idxxy] <= .0f)
					{
						float diff = abs(m_ppImg[z][idxxy]-m_fC[0]);
						sumC[0] += (diff*diff);
						numC[0]++;
					}	 
					else
					{
						float diff = abs(m_ppImg[z][idxxy]-m_fC[1]);
						sumC[1] += (diff*diff);
						numC[1]++;
					}					
				}
			}
		}
	}
	else
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int y = 0 ; y < m_nHeight ; y++)	
		{
			for (int x = 0 ; x < m_nWidth ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[0][idxxy] == 0)) continue;

				if (m_ppPhi[0][idxxy] <= .0f)
				{
					float diff = abs(m_ppImg[0][idxxy]-m_fC[0]);
					sumC[0] += (diff*diff);
					numC[0]++;
				}	 
				else
				{
					float diff = abs(m_ppImg[0][idxxy]-m_fC[1]);
					sumC[1] += (diff*diff);
					numC[1]++;
				}					
			}
		}
	}	
	energy = (sumC[0]/numC[0]) + (sumC[1]/numC[1]);

	return energy;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcEnergy(float** ppNew)
{
	float energy = .0f;
	int n = 0;
	if (m_nDepth > 1)
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int z = 0 ; z < m_nDepth ; z++)	
		{
			for (int y = 0 ; y < m_nHeight ; y++)	
			{
				for (int x = 0 ; x < m_nWidth ; x++)	
				{
					int idxxy = x + y*m_nWidth;
					if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

					float diff = abs(ppNew[z][idxxy]-m_ppPhi[z][idxxy]);
					energy += diff;
					n++;
				}
			}
		}
	}
	else
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int y = 0 ; y < m_nHeight ; y++)	
		{
			for (int x = 0 ; x < m_nWidth ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[0][idxxy] == 0)) continue;

				float diff = abs(ppNew[0][idxxy]-m_ppPhi[0][idxxy]);
				energy += diff;
				n++;
			}
		}
	}

	return (energy/n);
}


template<typename T> 
float CVpChanVeseACM<T>::PropagationL1(/*BYTE** ppOut*/)
{
	float energy = .0f;
	int zstart = 0;
	int zend = m_nDepth;
	if (m_nDepth > 1)
	{
		zstart = 1;
		zend = m_nDepth-1;
	}

	// 1. calc. median
	std::vector<T> vecC0;
	std::vector<T> vecC1;

#if defined OMP_PARALLEL
//	#pragma omp parallel for
#endif
	for (int z = zstart ; z < zend ; z++)	
	{
		for (int y = 1 ; y < m_nHeight-1 ; y++)	
		{
			for (int x = 1 ; x < m_nWidth-1 ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

				if (m_ppPhi[z][idxxy] < 0)
				{
					vecC0.push_back(m_ppImg[z][idxxy]);
				}
				else if (m_ppPhi[z][idxxy] > 0)
				{
					vecC1.push_back(m_ppImg[z][idxxy]);
				}
			}
		}
	}
	int nMidC0 = int(vecC0.size())>>1;
	int nMidC1 = int(vecC1.size())>>1;
	if ((nMidC0 == 0) || (nMidC1 == 0))
	{
		// ERRORR~~~!!!
		return	.0f;
	}

	nth_element(vecC0.begin(), vecC0.begin()+nMidC0, vecC0.end());
	nth_element(vecC1.begin(), vecC1.begin()+nMidC1, vecC1.end());
	

	// 2. update phi
	T c[2];
	c[0] = vecC0[nMidC0];
	c[1] = vecC1[nMidC1];

	float** ppNewPhi = NULL;
	if (!VirAlloc2D(ppNewPhi, m_nWH, m_nDepth)) return -1.0f;

	if (m_nDepth > 1)
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int z = 1 ; z < m_nDepth-1 ; z++)	
		{
			for (int y = 1 ; y < m_nHeight-1 ; y++)	
			{
				for (int x = 1 ; x < m_nWidth-1 ; x++)	
				{
					int idxxy = x + y*m_nWidth;
					if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

					if (m_fMu < EPSILON)
						ppNewPhi[z][idxxy] = CalcPhiL13DNoCurvature(x, y, z, c);
					else
						ppNewPhi[z][idxxy] = CalcPhiL13D(x, y, z, c);

				//	energy += ppNewPhi[z][idxxy];
				}
			}
		}
	}
	else
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int y = 1 ; y < m_nHeight-1 ; y++)	
		{
			for (int x = 1 ; x < m_nWidth-1 ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[0][idxxy] == 0)) continue;

				if (m_fMu < EPSILON)
					ppNewPhi[0][idxxy] = CalcPhiL12DNoCurvature(x, y, c);
				else
					ppNewPhi[0][idxxy] = CalcPhiL12D(x, y, c);

			//	energy += ppNewPhi[0][idxxy];
			}
		}
	}	
//	float energy = CalcEnergy(ppNewPhi);

	VirFree2D(m_ppPhi, m_nWH, m_nDepth);
	m_ppPhi = ppNewPhi;

	m_fC[0] = (float)c[0];
	m_fC[1] = (float)c[1];

//	float energy = CalcEnergy();

	return energy;
}


template<typename T> 
float CVpChanVeseACM<T>::PropagationL2(/*BYTE** ppOut*/)
{
	float energy = .0f;
	int zstart = 0;
	int zend = m_nDepth;
	if (m_nDepth > 1)
	{
		zstart = 1;
		zend = m_nDepth-1;
	}

	// 1. calc. mean
	float	c[2] = {.0f, .0f};
	int		cnt[2] = {0, 0};
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
	for (int z = zstart ; z < zend ; z++)	
	{
		for (int y = 1 ; y < m_nHeight-1 ; y++)	
		{
			for (int x = 1 ; x < m_nWidth-1 ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

				if (m_ppPhi[z][idxxy] < 0)
				{
					c[0] += (float)m_ppImg[z][idxxy];
					cnt[0]++;
				}
				else if (m_ppPhi[z][idxxy] > 0)
				{
					c[1] += (float)m_ppImg[z][idxxy];
					cnt[1]++;
				}
			}
		}
	}
	c[0] /= (float)cnt[0];
	c[1] /= (float)cnt[1];

	// 2. update phi
	float** ppNewPhi = NULL;
	if (!VirAlloc2D(ppNewPhi, m_nWH, m_nDepth)) return -1.0f;

	if (m_nDepth > 1)
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int z = 1 ; z < m_nDepth-1 ; z++)	
		{
			for (int y = 1 ; y < m_nHeight-1 ; y++)	
			{
				for (int x = 1 ; x < m_nWidth-1 ; x++)	
				{
					int idxxy = x + y*m_nWidth;
					if ((m_ppMask) && (m_ppMask[z][idxxy] == 0)) continue;

					if (m_fMu < EPSILON)
						ppNewPhi[z][idxxy] = CalcPhiL23DNoCurvature(x, y, z, c);
					else
						ppNewPhi[z][idxxy] = CalcPhiL23D(x, y, z, c);

				//	energy += ppNewPhi[z][idxxy];
				}
			}
		}
	}
	else
	{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
		for (int y = 1 ; y < m_nHeight-1 ; y++)	
		{
			for (int x = 1 ; x < m_nWidth-1 ; x++)	
			{
				int idxxy = x + y*m_nWidth;
				if ((m_ppMask) && (m_ppMask[0][idxxy] == 0)) continue;

				if (m_fMu < EPSILON)
					ppNewPhi[0][idxxy] = CalcPhiL22DNoCurvature(x, y, c);
				else
					ppNewPhi[0][idxxy] = CalcPhiL22D(x, y, c);

			//	energy += ppNewPhi[0][idxxy];
			}
		}
	}
//	float energy = CalcEnergy(ppNewPhi);

	VirFree2D(m_ppPhi, m_nWH, m_nDepth);
	m_ppPhi = ppNewPhi;

	m_fC[0] = (float)c[0];
	m_fC[1] = (float)c[1];

//	float energy = CalcEnergy();	

	return energy;
}

template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL12D(int i, int j, T* median)
{
	int idxij = i+j*m_nWidth;

	float phi0 = m_ppPhi[0][idxij-1+m_nWidth];
	float phi1 = m_ppPhi[0][idxij+m_nWidth];
	float phi2 = m_ppPhi[0][idxij+1+m_nWidth];
	float phi3 = m_ppPhi[0][idxij-1];
	float phi4 = m_ppPhi[0][idxij];
	float phi5 = m_ppPhi[0][idxij+1];
	float phi6 = m_ppPhi[0][idxij-1-m_nWidth];
	float phi7 = m_ppPhi[0][idxij-m_nWidth];
	float phi8 = m_ppPhi[0][idxij+1-m_nWidth];

	float fx = ( phi5 - phi3 )/(2*m_nH[0]);
	float fy = ( phi1 - phi7 )/(2*m_nH[1]);
	float grad = pow(fx*fx + fy*fy, 0.5f);
	float Eext = m_fLamda*(abs(m_ppImg[0][idxij]-median[0]) - abs(m_ppImg[0][idxij]-median[1]));

	int h0_2 = m_nH[0]*m_nH[0];
	int h1_2 = m_nH[1]*m_nH[1];

	float fxx = (phi5 - 2*phi4 + phi3) / h0_2;
	float fyy = (phi1 - 2*phi4 + phi7) / h1_2;
	float fxy = (phi2 - phi8 - phi0 + phi6) / (4.0f*m_nH[1]*m_nH[0]);

//	float d1 = float(1.0f/(m_nH[0]*pow(pow((phi5-phi4)/m_nH[0],2) + pow((phi1+phi2-phi7-phi8)/(4.0f*m_nH[1]) ,2), 0.5f) + EPSILON));
//	float d2 = float(1.0f/(m_nH[0]*pow(pow((phi3-phi4)/m_nH[0],2) + pow((phi1+phi0-phi7-phi6)/(4.0f*m_nH[1]) ,2), 0.5f) + EPSILON));
//	float d3 = float(1.0f/(m_nH[1]*pow(pow((phi1-phi4)/m_nH[1],2) + pow((phi2+phi5-phi0-phi3)/(4.0f*m_nH[0]) ,2), 0.5f) + EPSILON));
//	float d4 = float(1.0f/(m_nH[1]*pow(pow((phi7-phi4)/m_nH[1],2) + pow((phi5+phi8-phi3-phi6)/(4.0f*m_nH[0]) ,2), 0.5f) + EPSILON));
	float d1 = float(1.0f/(m_nH[0]*pow((phi5-phi4)*(phi5-phi4)/h0_2 + (phi1+phi2-phi7-phi8)*(phi1+phi2-phi7-phi8)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d2 = float(1.0f/(m_nH[0]*pow((phi3-phi4)*(phi3-phi4)/h0_2 + (phi1+phi0-phi7-phi6)*(phi1+phi0-phi7-phi6)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d3 = float(1.0f/(m_nH[1]*pow((phi1-phi4)*(phi1-phi4)/h1_2 + (phi2+phi5-phi0-phi3)*(phi2+phi5-phi0-phi3)/(16.0f*h0_2), 0.5f) + EPSILON));
	float d4 = float(1.0f/(m_nH[1]*pow((phi7-phi4)*(phi7-phi4)/h1_2 + (phi5+phi8-phi3-phi6)*(phi5+phi8-phi3-phi6)/(16.0f*h0_2), 0.5f) + EPSILON));

	float D = m_fMu*(d1+d2+d3+d4);
	float E = m_fMu*(d1*phi5 + d2*phi3 + d3*phi1 + d4*phi7);
	float nextphi = (phi4 + grad*m_fDeltaT*(E + Eext))/(1+m_fDeltaT*grad*D);

	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL22D(int i, int j, float* mean)
{
	int idxij = i+j*m_nWidth;

	float phi0 = m_ppPhi[0][idxij-1+m_nWidth];
	float phi1 = m_ppPhi[0][idxij+m_nWidth];
	float phi2 = m_ppPhi[0][idxij+1+m_nWidth];
	float phi3 = m_ppPhi[0][idxij-1];
	float phi4 = m_ppPhi[0][idxij];
	float phi5 = m_ppPhi[0][idxij+1];
	float phi6 = m_ppPhi[0][idxij-1-m_nWidth];
	float phi7 = m_ppPhi[0][idxij-m_nWidth];
	float phi8 = m_ppPhi[0][idxij+1-m_nWidth];

	float fx = ( phi5-phi3 )/(2*m_nH[0]);
	float fy = ( phi1 - phi7 )/(2*m_nH[1]);
	float grad = pow(fx*fx + fy*fy, 0.5f);
	float Eext = m_fLamda*((m_ppImg[0][idxij]-mean[0])*(m_ppImg[0][idxij]-mean[0]) - (m_ppImg[0][idxij]-mean[1])*(m_ppImg[0][idxij]-mean[1]));

	int h0_2 = m_nH[0]*m_nH[0];
	int h1_2 = m_nH[1]*m_nH[1];

	float fxx = (phi5 - 2*phi4 + phi3) / h0_2;
	float fyy = (phi1 - 2*phi4 + phi7) / h1_2;
	float fxy = (phi2 - phi8 - phi0 + phi6) / (4.0f*m_nH[1]*m_nH[0]);

// 	float d1 = float(1.0f/(m_nH[0]*pow(pow((phi5-phi4)/m_nH[0],2) + pow((phi1+phi2-phi7-phi8)/(4.0f*m_nH[1]) ,2), 0.5f) + EPSILON));
// 	float d2 = float(1.0f/(m_nH[0]*pow(pow((phi3-phi4)/m_nH[0],2) + pow((phi1+phi0-phi7-phi6)/(4.0f*m_nH[1]) ,2), 0.5f) + EPSILON));
// 	float d3 = float(1.0f/(m_nH[1]*pow(pow((phi1-phi4)/m_nH[1],2) + pow((phi2+phi5-phi0-phi3)/(4.0f*m_nH[0]) ,2), 0.5f) + EPSILON));
// 	float d4 = float(1.0f/(m_nH[1]*pow(pow((phi7-phi4)/m_nH[1],2) + pow((phi5+phi8-phi3-phi6)/(4.0f*m_nH[0]) ,2), 0.5f) + EPSILON));
	float d1 = float(1.0f/(m_nH[0]*pow((phi5-phi4)*(phi5-phi4)/h0_2 + (phi1+phi2-phi7-phi8)*(phi1+phi2-phi7-phi8)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d2 = float(1.0f/(m_nH[0]*pow((phi3-phi4)*(phi3-phi4)/h0_2 + (phi1+phi0-phi7-phi6)*(phi1+phi0-phi7-phi6)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d3 = float(1.0f/(m_nH[1]*pow((phi1-phi4)*(phi1-phi4)/h1_2 + (phi2+phi5-phi0-phi3)*(phi2+phi5-phi0-phi3)/(16.0f*h0_2), 0.5f) + EPSILON));
	float d4 = float(1.0f/(m_nH[1]*pow((phi7-phi4)*(phi7-phi4)/h1_2 + (phi5+phi8-phi3-phi6)*(phi5+phi8-phi3-phi6)/(16.0f*h0_2), 0.5f) + EPSILON));

	float D = m_fMu*(d1+d2+d3+d4);
	float E = m_fMu*(d1*phi5 + d2*phi3 + d3*phi1 + d4*phi7);
	float nextphi = (phi4 + grad*m_fDeltaT*(E + Eext))/(1+m_fDeltaT*grad*D);

	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL13D(int i, int j, int k, T* median)
{
	int idxij = i + j*m_nWidth;

	float phi1 = m_ppPhi[k-1][idxij+m_nWidth];
	float phi3 = m_ppPhi[k-1][idxij-1];
	float phi4 = m_ppPhi[k-1][idxij];
	float phi5 = m_ppPhi[k-1][idxij+1];
	float phi7 = m_ppPhi[k-1][idxij-m_nWidth];

	float phi9	= m_ppPhi[k][idxij-1+m_nWidth];
	float phi10 = m_ppPhi[k][idxij+m_nWidth];
	float phi11 = m_ppPhi[k][idxij+1+m_nWidth];
	float phi12 = m_ppPhi[k][idxij-1];
	float phi13 = m_ppPhi[k][idxij];
	float phi14 = m_ppPhi[k][idxij+1];
	float phi15 = m_ppPhi[k][idxij-1-m_nWidth];
	float phi16 = m_ppPhi[k][idxij-m_nWidth];
	float phi17 = m_ppPhi[k][idxij+1-m_nWidth];

	float phi19 = m_ppPhi[k+1][idxij+m_nWidth];
	float phi21 = m_ppPhi[k+1][idxij-1];
	float phi22 = m_ppPhi[k+1][idxij];
	float phi23 = m_ppPhi[k+1][idxij+1];
	float phi25 = m_ppPhi[k+1][idxij-m_nWidth];

	float fx = ( phi14 -phi12 )/(2*m_nH[0]);
	float fy = ( phi10 - phi16 )/(2*m_nH[1]);
	float fz = ( phi22 - phi4 )/(2*m_nH[2]);
	float grad = pow(fx*fx + fy*fy + fz*fz, 0.5f);

	float Eext = m_fLamda*(abs(m_ppImg[k][idxij]-median[0]) - abs(m_ppImg[k][idxij]-median[1]));

// 	float d1 = float(1.0f/(m_nH[0]*pow(pow((phi14-phi13)/m_nH[0],2) + pow((phi10+phi11-phi16-phi17)/(4.0f*m_nH[1]), 2) + pow((phi22+phi23-phi4-phi5)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d2 = float(1.0f/(m_nH[0]*pow(pow((phi13-phi12)/m_nH[0],2) + pow((phi9+phi10-phi15-phi16)/(4.0f*m_nH[1]),2) + pow((phi21+phi22-phi3-phi4)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d3 = float(1.0f/(m_nH[1]*pow(pow((phi10-phi13)/m_nH[1],2) + pow((phi14+phi11-phi12-phi9)/(4.0f*m_nH[0]),2) + pow((phi22+phi19-phi4-phi1)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d4 = float(1.0f/(m_nH[1]*pow(pow((phi13-phi16)/m_nH[1],2) + pow((phi17+phi14-phi15-phi12)/(4.0f*m_nH[0]),2) + pow((phi25+phi22-phi7-phi4)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d5 = float(1.0f/(m_nH[2]*pow(pow((phi22-phi13)/m_nH[2],2) + pow((phi14+phi23-phi12-phi21)/(4.0f*m_nH[0]),2) + pow((phi10+phi19-phi16-phi25)/(4.0f*m_nH[1]),2), 0.5f) + EPSILON));
// 	float d6 = float(1.0f/(m_nH[2]*pow(pow((phi13-phi4)/m_nH[2],2) + pow((phi5+phi14-phi3-phi12)/(4.0f*m_nH[0]),2) + pow((phi1+phi10-phi7-phi16)/(4.0f*m_nH[1]),2), 0.5f) + EPSILON));

	int h0_2 = m_nH[0]*m_nH[0];
	int h1_2 = m_nH[1]*m_nH[1];
	int h2_2 = m_nH[2]*m_nH[2];
	float d1 = float(1.0f/(m_nH[0]*pow((phi14-phi13)*(phi14-phi13)/h0_2 + (phi10+phi11-phi16-phi17)*(phi10+phi11-phi16-phi17)/(16.0f*h1_2) + (phi22+phi23-phi4-phi5)*(phi22+phi23-phi4-phi5)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d2 = float(1.0f/(m_nH[0]*pow((phi13-phi12)*(phi13-phi12)/h0_2 + (phi9+phi10-phi15-phi16)*(phi9+phi10-phi15-phi16)/(16.0f*h1_2) + (phi21+phi22-phi3-phi4)*(phi21+phi22-phi3-phi4)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d3 = float(1.0f/(m_nH[1]*pow((phi10-phi13)*(phi10-phi13)/h1_2 + (phi14+phi11-phi12-phi9)*(phi14+phi11-phi12-phi9)/(16.0f*h0_2) + (phi22+phi19-phi4-phi1)*(phi22+phi19-phi4-phi1)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d4 = float(1.0f/(m_nH[1]*pow((phi13-phi16)*(phi13-phi16)/h1_2 + (phi17+phi14-phi15-phi12)*(phi17+phi14-phi15-phi12)/(16.0f*h0_2) + (phi25+phi22-phi7-phi4)*(phi25+phi22-phi7-phi4)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d5 = float(1.0f/(m_nH[2]*pow((phi22-phi13)*(phi22-phi13)/h2_2 + (phi14+phi23-phi12-phi21)*(phi14+phi23-phi12-phi21)/(16.0f*h0_2) + (phi10+phi19-phi16-phi25)*(phi10+phi19-phi16-phi25)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d6 = float(1.0f/(m_nH[2]*pow((phi13-phi4)*(phi13-phi4)/h2_2 + (phi5+phi14-phi3-phi12)*(phi5+phi14-phi3-phi12)/(16.0f*h0_2) + (phi1+phi10-phi7-phi16)*(phi1+phi10-phi7-phi16)/(16.0f*h1_2), 0.5f) + EPSILON));

	float D = m_fMu*(d1+d2+d3+d4+d5+d6);
	float E = m_fMu*(d1*phi14 + d2*phi12 + d3*phi10 + d4*phi16 + d5*phi22 + d6*phi4);
	float nextphi = (phi13 + grad*m_fDeltaT*(E + Eext)) / (1+m_fDeltaT*grad*D);

	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL23D(int i, int j, int k, float* mean)
{
	int idxij = i + j*m_nWidth;

	float phi1 = m_ppPhi[k-1][idxij+m_nWidth];
	float phi3 = m_ppPhi[k-1][idxij-1];
	float phi4 = m_ppPhi[k-1][idxij];
	float phi5 = m_ppPhi[k-1][idxij+1];
	float phi7 = m_ppPhi[k-1][idxij-m_nWidth];

	float phi9	= m_ppPhi[k][idxij-1+m_nWidth];
	float phi10 = m_ppPhi[k][idxij+m_nWidth];
	float phi11 = m_ppPhi[k][idxij+1+m_nWidth];
	float phi12 = m_ppPhi[k][idxij-1];
	float phi13 = m_ppPhi[k][idxij];
	float phi14 = m_ppPhi[k][idxij+1];
	float phi15 = m_ppPhi[k][idxij-1-m_nWidth];
	float phi16 = m_ppPhi[k][idxij-m_nWidth];
	float phi17 = m_ppPhi[k][idxij+1-m_nWidth];

	float phi19 = m_ppPhi[k+1][idxij+m_nWidth];
	float phi21 = m_ppPhi[k+1][idxij-1];
	float phi22 = m_ppPhi[k+1][idxij];
	float phi23 = m_ppPhi[k+1][idxij+1];
	float phi25 = m_ppPhi[k+1][idxij-m_nWidth];

	float fx = ( phi14 -phi12 )/(2*m_nH[0]);
	float fy = ( phi10 - phi16 )/(2*m_nH[1]);
	float fz = ( phi22 - phi4 )/(2*m_nH[2]);
	float grad = pow(fx*fx + fy*fy + fz*fz, 0.5f);

	float Eext = m_fLamda*((m_ppImg[k][idxij]-mean[0])*(m_ppImg[k][idxij]-mean[0]) - (m_ppImg[k][idxij]-mean[1])*(m_ppImg[k][idxij]-mean[1]));

// 	float d1 = float(1.0f/(m_nH[0]*pow(pow((phi14-phi13)/m_nH[0],2) + pow((phi10+phi11-phi16-phi17)/(4.0f*m_nH[1]), 2) + pow((phi22+phi23-phi4-phi5)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d2 = float(1.0f/(m_nH[0]*pow(pow((phi13-phi12)/m_nH[0],2) + pow((phi9+phi10-phi15-phi16)/(4.0f*m_nH[1]),2) + pow((phi21+phi22-phi3-phi4)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d3 = float(1.0f/(m_nH[1]*pow(pow((phi10-phi13)/m_nH[1],2) + pow((phi14+phi11-phi12-phi9)/(4.0f*m_nH[0]),2) + pow((phi22+phi19-phi4-phi1)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d4 = float(1.0f/(m_nH[1]*pow(pow((phi13-phi16)/m_nH[1],2) + pow((phi17+phi14-phi15-phi12)/(4.0f*m_nH[0]),2) + pow((phi25+phi22-phi7-phi4)/(4.0f*m_nH[2]),2), 0.5f) + EPSILON));
// 	float d5 = float(1.0f/(m_nH[2]*pow(pow((phi22-phi13)/m_nH[2],2) + pow((phi14+phi23-phi12-phi21)/(4.0f*m_nH[0]),2) + pow((phi10+phi19-phi16-phi25)/(4.0f*m_nH[1]),2), 0.5f) + EPSILON));
// 	float d6 = float(1.0f/(m_nH[2]*pow(pow((phi13-phi4)/m_nH[2],2) + pow((phi5+phi14-phi3-phi12)/(4.0f*m_nH[0]),2) + pow((phi1+phi10-phi7-phi16)/(4.0f*m_nH[1]),2), 0.5f) + EPSILON));

	int h0_2 = m_nH[0]*m_nH[0];
	int h1_2 = m_nH[1]*m_nH[1];
	int h2_2 = m_nH[2]*m_nH[2];
	float d1 = float(1.0f/(m_nH[0]*pow((phi14-phi13)*(phi14-phi13)/h0_2 + (phi10+phi11-phi16-phi17)*(phi10+phi11-phi16-phi17)/(16.0f*h1_2) + (phi22+phi23-phi4-phi5)*(phi22+phi23-phi4-phi5)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d2 = float(1.0f/(m_nH[0]*pow((phi13-phi12)*(phi13-phi12)/h0_2 + (phi9+phi10-phi15-phi16)*(phi9+phi10-phi15-phi16)/(16.0f*h1_2) + (phi21+phi22-phi3-phi4)*(phi21+phi22-phi3-phi4)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d3 = float(1.0f/(m_nH[1]*pow((phi10-phi13)*(phi10-phi13)/h1_2 + (phi14+phi11-phi12-phi9)*(phi14+phi11-phi12-phi9)/(16.0f*h0_2) + (phi22+phi19-phi4-phi1)*(phi22+phi19-phi4-phi1)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d4 = float(1.0f/(m_nH[1]*pow((phi13-phi16)*(phi13-phi16)/h1_2 + (phi17+phi14-phi15-phi12)*(phi17+phi14-phi15-phi12)/(16.0f*h0_2) + (phi25+phi22-phi7-phi4)*(phi25+phi22-phi7-phi4)/(16.0f*h2_2), 0.5f) + EPSILON));
	float d5 = float(1.0f/(m_nH[2]*pow((phi22-phi13)*(phi22-phi13)/h2_2 + (phi14+phi23-phi12-phi21)*(phi14+phi23-phi12-phi21)/(16.0f*h0_2) + (phi10+phi19-phi16-phi25)*(phi10+phi19-phi16-phi25)/(16.0f*h1_2), 0.5f) + EPSILON));
	float d6 = float(1.0f/(m_nH[2]*pow((phi13-phi4)*(phi13-phi4)/h2_2 + (phi5+phi14-phi3-phi12)*(phi5+phi14-phi3-phi12)/(16.0f*h0_2) + (phi1+phi10-phi7-phi16)*(phi1+phi10-phi7-phi16)/(16.0f*h1_2), 0.5f) + EPSILON));

	float D = m_fMu*(d1+d2+d3+d4+d5+d6);
	float E = m_fMu*(d1*phi14 + d2*phi12 + d3*phi10 + d4*phi16 + d5*phi22 + d6*phi4);
	float nextphi = (phi13 + grad*m_fDeltaT*(E + Eext)) / (1+m_fDeltaT*grad*D);
	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL12DNoCurvature(int i, int j, T* median)
{
	int idxij = i+j*m_nWidth;

	float phi0 = m_ppPhi[0][idxij-1+m_nWidth];
	float phi1 = m_ppPhi[0][idxij+m_nWidth];
	float phi2 = m_ppPhi[0][idxij+1+m_nWidth];
	float phi3 = m_ppPhi[0][idxij-1];
	float phi4 = m_ppPhi[0][idxij];
	float phi5 = m_ppPhi[0][idxij+1];
	float phi6 = m_ppPhi[0][idxij-1-m_nWidth];
	float phi7 = m_ppPhi[0][idxij-m_nWidth];
	float phi8 = m_ppPhi[0][idxij+1-m_nWidth];

	float fx = ( phi5-phi3 )/(2*m_nH[0]);
	float fy = ( phi1 - phi7 )/(2*m_nH[1]);
	float grad = pow(fx*fx + fy*fy, 0.5f);
	
	float fxx = (phi5 - 2*phi4 + phi3) / (m_nH[0]*m_nH[0]);
	float fyy = (phi1 - 2*phi4 + phi7) / (m_nH[1]*m_nH[1]);
	float fxy = (phi2 - phi8 - phi0 + phi6) / (4.0f*m_nH[1]*m_nH[0]);

	float Eext = m_fLamda*(abs(m_ppImg[0][idxij]-median[0]) - abs(m_ppImg[0][idxij]-median[1]));
	float nextphi = (phi4 + grad*m_fDeltaT*Eext);

	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL22DNoCurvature(int i, int j, float* mean)
{
	int idxij = i+j*m_nWidth;

	float phi0 = m_ppPhi[0][idxij-1+m_nWidth];
	float phi1 = m_ppPhi[0][idxij+m_nWidth];
	float phi2 = m_ppPhi[0][idxij+1+m_nWidth];
	float phi3 = m_ppPhi[0][idxij-1];
	float phi4 = m_ppPhi[0][idxij];
	float phi5 = m_ppPhi[0][idxij+1];
	float phi6 = m_ppPhi[0][idxij-1-m_nWidth];
	float phi7 = m_ppPhi[0][idxij-m_nWidth];
	float phi8 = m_ppPhi[0][idxij+1-m_nWidth];

	float fx = ( phi5-phi3 )/(2*m_nH[0]);
	float fy = ( phi1 - phi7 )/(2*m_nH[1]);
	float grad = pow(fx*fx + fy*fy, 0.5f);

	float fxx = (phi5 - 2*phi4 + phi3) / (m_nH[0]*m_nH[0]);
	float fyy = (phi1 - 2*phi4 + phi7) / (m_nH[1]*m_nH[1]);
	float fxy = (phi2 - phi8 - phi0 + phi6) / (4.0f*m_nH[1]*m_nH[0]);

	float Eext = m_fLamda*((m_ppImg[0][idxij]-mean[0])*(m_ppImg[0][idxij]-mean[0]) - (m_ppImg[0][idxij]-mean[1])*(m_ppImg[0][idxij]-mean[1]));
	float nextphi = (phi4 + grad*m_fDeltaT*Eext);

	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL13DNoCurvature(int i, int j, int k, T* median)
{
	int idxij = i + j*m_nWidth;

	float phi1 = m_ppPhi[k-1][idxij+m_nWidth];
	float phi3 = m_ppPhi[k-1][idxij-1];
	float phi4 = m_ppPhi[k-1][idxij];
	float phi5 = m_ppPhi[k-1][idxij+1];
	float phi7 = m_ppPhi[k-1][idxij-m_nWidth];

	float phi9	= m_ppPhi[k][idxij-1+m_nWidth];
	float phi10 = m_ppPhi[k][idxij+m_nWidth];
	float phi11 = m_ppPhi[k][idxij+1+m_nWidth];
	float phi12 = m_ppPhi[k][idxij-1];
	float phi13 = m_ppPhi[k][idxij];
	float phi14 = m_ppPhi[k][idxij+1];
	float phi15 = m_ppPhi[k][idxij-1-m_nWidth];
	float phi16 = m_ppPhi[k][idxij-m_nWidth];
	float phi17 = m_ppPhi[k][idxij+1-m_nWidth];

	float phi19 = m_ppPhi[k+1][idxij+m_nWidth];
	float phi21 = m_ppPhi[k+1][idxij-1];
	float phi22 = m_ppPhi[k+1][idxij];
	float phi23 = m_ppPhi[k+1][idxij+1];
	float phi25 = m_ppPhi[k+1][idxij-m_nWidth];

	float fx = ( phi14 -phi12 )/(2*m_nH[0]);
	float fy = ( phi10 - phi16 )/(2*m_nH[1]);
	float fz = ( phi22 - phi4 )/(2*m_nH[2]);
	float grad = pow(fx*fx + fy*fy + fz*fz, 0.5f);

	float Eext = m_fLamda*(abs(m_ppImg[k][idxij]-median[0]) - abs(m_ppImg[k][idxij]-median[1]));
	float nextphi = (phi13 + grad*m_fDeltaT*Eext);
	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


template<typename T> 
float CVpChanVeseACM<T>::CalcPhiL23DNoCurvature(int i, int j, int k, float* mean)
{
	int idxij = i + j*m_nWidth;

	float phi1 = m_ppPhi[k-1][idxij+m_nWidth];
	float phi3 = m_ppPhi[k-1][idxij-1];
	float phi4 = m_ppPhi[k-1][idxij];
	float phi5 = m_ppPhi[k-1][idxij+1];
	float phi7 = m_ppPhi[k-1][idxij-m_nWidth];

	float phi9	= m_ppPhi[k][idxij-1+m_nWidth];
	float phi10 = m_ppPhi[k][idxij+m_nWidth];
	float phi11 = m_ppPhi[k][idxij+1+m_nWidth];
	float phi12 = m_ppPhi[k][idxij-1];
	float phi13 = m_ppPhi[k][idxij];
	float phi14 = m_ppPhi[k][idxij+1];
	float phi15 = m_ppPhi[k][idxij-1-m_nWidth];
	float phi16 = m_ppPhi[k][idxij-m_nWidth];
	float phi17 = m_ppPhi[k][idxij+1-m_nWidth];

	float phi19 = m_ppPhi[k+1][idxij+m_nWidth];
	float phi21 = m_ppPhi[k+1][idxij-1];
	float phi22 = m_ppPhi[k+1][idxij];
	float phi23 = m_ppPhi[k+1][idxij+1];
	float phi25 = m_ppPhi[k+1][idxij-m_nWidth];

	float fx = ( phi14 -phi12 )/(2*m_nH[0]);
	float fy = ( phi10 - phi16 )/(2*m_nH[1]);
	float fz = ( phi22 - phi4 )/(2*m_nH[2]);
	float grad = pow(fx*fx + fy*fy + fz*fz, 0.5f);

	float Eext = m_fLamda*((m_ppImg[k][idxij]-mean[0])*(m_ppImg[k][idxij]-mean[0]) - (m_ppImg[k][idxij]-mean[1])*(m_ppImg[k][idxij]-mean[1]));
	float nextphi = (phi13 + grad*m_fDeltaT*Eext);
	nextphi = Max(-100.0f, nextphi);
	nextphi = Min(100.0f, nextphi);

	return nextphi;
}


// 0 : ignored
// 1 : background
// 2 : object
template<typename T> 
void CVpChanVeseACM<T>::SaveResults(BYTE** ppOut)
{
	int zstart = 0;
	int zend = m_nDepth;
	if (m_nDepth > 1)
	{
		zstart = 1;
		zend = m_nDepth-1;
	}

	// 1. set value
	BYTE less0, greater0;
	if (m_fC[0] > m_fC[1]) 
	{
		less0 = OBJECT;
		greater0 = BACKGRND;
	}
	else
	{
		less0 = BACKGRND;
		greater0 = OBJECT;
	}

#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
	for (int z = zstart ; z < zend ; z++)	
	{
		for (int y = 0 ; y < m_nHeight ; y++)	
		{
			for (int x = 0 ; x < m_nWidth ; x++)	
			{					
				int idxxy = x + y*m_nWidth;
				if (m_ppMask)
				{
					if (m_ppMask[z][idxxy] == 0)
						ppOut[z][idxxy] = IGNORED;
					else if (m_ppPhi[z][idxxy] <= .0f)							
						ppOut[z][idxxy] = less0;
					else
						ppOut[z][idxxy] = greater0;

				}
				else
				{
					if (m_ppPhi[z][idxxy] <= .0f)	
						ppOut[z][idxxy] = less0;
					else							
						ppOut[z][idxxy] = greater0;
				}				
			}
		}
	}
}


template<typename T> 
void CVpChanVeseACM<T>::SetOnlyObject(BYTE** ppOut)
{
#if defined OMP_PARALLEL
	#pragma omp parallel for
#endif
	for (int z = 0 ; z < m_nDepth ; z++)	
	{
		for (int xy = 0 ; xy < m_nWH ; xy++)	
		{
			if (ppOut[z][xy] < 2)	ppOut[z][xy] = 0;
			else					ppOut[z][xy] = 255;
		}
	}	
}

template<typename T> 
void CVpChanVeseACM<T>::SetObjBackgrnd(BYTE** ppOut)
{
#if defined OMP_PARALLEL
#pragma omp parallel for
#endif
	for (int z = 0 ; z < m_nDepth ; z++)	
	{
		for (int xy = 0 ; xy < m_nWH ; xy++)	
		{
			if (ppOut[z][xy] == 0)	ppOut[z][xy] = 0;
			else					ppOut[z][xy] = 255;
		}
	}	
}


template<typename T> 
BOOL CVpChanVeseACM<T>::Do(BYTE** ppOut)
{
	float curenergy;
	float oldenergy = FLT_MAX;
	
	for (int i = 0 ; i < m_nIter ; i++)
	{
		switch (m_eFidelity)
		{
		case L1:
			curenergy = PropagationL1(/*ppOut*/);
			break;

		case L2:
			curenergy = PropagationL2(/*ppOut*/);
			break;

		default:
			break;
		}

		// stop condition
		//if (abs(oldenergy-curenergy) < EPSILON)	break;
		oldenergy = curenergy;	

		//
		// todo : reinitialization
		//
	}

	SaveResults(ppOut);

	return TRUE;
}


template<typename T>
void CVpChanVeseACM<T>::Release()
{
	CVpACM<T>::Release();
}