#include "StdAfx.h"
#include "EnergyFunctionWrap.h"

namespace ManagedMRF {

	EnergyFunctionWrap::EnergyFunctionWrap(DataCost* data, SmoothnessCost* smooth)
	{
		pDataCost = data;
		pSmoothCost = smooth;
	}
}

