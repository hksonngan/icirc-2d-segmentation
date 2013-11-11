#pragma once

#include "GCoptimization.h"

namespace ManagedMRF {

	public ref class GraphCutWrap
	{
	public:
		GraphCutWrap(void);
		GraphCutWrap(int XNum, int YNum, EnergyFunction *eng, bool IsExpansion);
	private:
		GCoptimization*	pGC;
	};

}


