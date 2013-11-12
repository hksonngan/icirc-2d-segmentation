#include "StdAfx.h"
#include "GraphCutWrap.h"
#include "GCoptimization.h"

namespace ManagedMRF {

	GraphCutWrap::GraphCutWrap(int XNum, int YNum, double DataEnergy[], double SmoothnessLabel[], 
			double SmoothnessHorizontal[], double SmoothnessVertical[], bool IsSwap)
	{
		DataCost* data         = new DataCost(DataEnergy);
		SmoothnessCost* smooth = new SmoothnessCost(SmoothnessLabel, SmoothnessHorizontal, SmoothnessVertical);
		EnergyFunction* pEnergyFunc = new EnergyFunction(data, smooth);
		if (IsSwap)
			pGC = new Swap(XNum, YNum, 2, pEnergyFunc);
		else
			pGC = new Expansion(XNum, YNum, 2, pEnergyFunc);
	}

	void GraphCutWrap::OptimizeOneIteration()
	{
		float TimeTemp;
		pGC->optimize(1, TimeTemp);
	}
}