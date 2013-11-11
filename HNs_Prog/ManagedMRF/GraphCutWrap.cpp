#include "StdAfx.h"
#include "GraphCutWrap.h"

namespace ManagedMRF {

	GraphCutWrap::GraphCutWrap(void)
	{
		pGC = NULL;
	}

	GraphCutWrap::GraphCutWrap(int XNum, int YNum, EnergyFunction *eng, bool IsSwap)
	{
		if (IsSwap)
		{
			pGC = new Swap(XNum, YNum, 2, eng);
		}
		else
		{
			pGC = new Expansion(XNum, YNum, 2, eng);
		}
	}
}