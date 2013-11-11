#pragma once

#include "mrf.h"
namespace ManagedMRF {

	public ref class EnergyFunctionWrap
	{
	public:
		EnergyFunctionWrap(DataCost* data, SmoothnessCost* smooth);

	private:
		DataCost*		pDataCost;
		SmoothnessCost* pSmoothCost;
	};

}

