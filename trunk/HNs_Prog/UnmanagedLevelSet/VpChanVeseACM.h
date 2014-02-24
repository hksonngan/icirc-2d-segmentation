#pragma once
#include "VpACM.h"

#define	DEFAULT_MU				1.0f
#define	DEFAULT_LAMDA			1.0f
#define	DEFAULT_EDGETERMWEIGHT	1.0f
#define	EPSILON					0.0000001

template <typename T>
class CVpChanVeseACM : public CVpACM<T>
{
public:
	enum eFidelity
	{
		L1 = 0,
		L2 
	};

		
private:
	eInit		m_eInit;
	eFidelity	m_eFidelity;
	float m_fMu;
	float m_fLamda;
	float m_fC[2];
	float m_fEdgeTermWeight;

	float PropagationL1(/*BYTE** ppOut*/);
	float PropagationL2(/*BYTE** ppOut*/);
	float PropagationL1Edge(/*BYTE** ppOut*/);
	float PropagationL2Edge(/*BYTE** ppOut*/);

	float CalcPhiL12D(int i, int j, T* median);
	float CalcPhiL22D(int i, int j, float* mean);
	float CalcPhiL13D(int i, int j, int k, T* median);
	float CalcPhiL23D(int i, int j, int k, float* mean);

	float CalcPhiL12DNoCurvature(int i, int j, T* median);
	float CalcPhiL22DNoCurvature(int i, int j, float* mean);
	float CalcPhiL13DNoCurvature(int i, int j, int k, T* median);
	float CalcPhiL23DNoCurvature(int i, int j, int k, float* mean);

	void SaveResults(BYTE** ppOut);	

	float CalcEnergy();
	float CalcEnergy(float** ppNew);

public:
	CVpChanVeseACM();	
	virtual ~CVpChanVeseACM();
	
inline	void InitParam(float mu = DEFAULT_MU, float lamda = DEFAULT_LAMDA, float deltat = DEFAULT_DELTAT, int hx = DEFAULT_H, int hy = DEFAULT_H, int hz = DEFAULT_H, 
				  int nIter = DEFAULT_ITERATION, eFidelity efd = L2)	{m_fMu = mu; m_fLamda = lamda; m_fDeltaT = deltat; m_nH[0] = hx; m_nH[1] = hy; m_nH[2] = hz; m_nIter = nIter; m_eFidelity = efd;}

	virtual BOOL Do(BYTE** ppOut);
	virtual void Release();	

	void SetOnlyObject(BYTE** ppOut);
	void SetObjBackgrnd(BYTE** ppOut);

inline void SetEdgeTerm(float weight) {m_fEdgeTermWeight = weight;}

};

template class CVpChanVeseACM<BYTE>;
template class CVpChanVeseACM<short>;

