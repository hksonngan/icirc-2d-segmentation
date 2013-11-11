#include "StdAfx.h"
#include "EnergyFunctionWrap.h"

namespace ManagedMRF {

	EnergyFunctionWrap::EnergyFunctionWrap(double DataEnergy[], double SmoothnessLabel[], double SmoothnessHorizontal[], double SmoothnessVertical[])
	{
		DataCost* data         = new DataCost(DataEnergy);
		SmoothnessCost* smooth = new SmoothnessCost(SmoothnessLabel, SmoothnessHorizontal, SmoothnessVertical);
		pEnergyFunc = new EnergyFunction(data, smooth);
	}
}

