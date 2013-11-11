#pragma once

#include "mrf.h"
namespace ManagedMRF {

	public ref class EnergyFunctionWrap
	{
	public:
		EnergyFunctionWrap(double DataEnergy[], double SmoothnessLabel[], double SmoothnessHorizontal[], double SmoothnessVertical[]);
	private:
		EnergyFunction*	pEnergyFunc;
	};
}

