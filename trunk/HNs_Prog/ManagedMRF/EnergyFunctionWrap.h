#pragma once

#include "mrf.h"
namespace ManagedMRF {

	public ref class DataCostWrap
	{
	public:
		DataCostWrap(double DataEneryArray[]);

	public:
		DataCost*	pEnergyFunc;
	};

	public ref class SmoothnessCostWrap
	{
	public:
		SmoothnessCostWrap();

	public:
		SmoothnessCost*		pEnergyFunc;
	//private:
	//	double CostFunction(int pix1, int pix2, int i, int j);
	};

	public ref class EnergyFunctionWrap
	{
	public:
		EnergyFunctionWrap(DataCostWrap data, SmoothnessCostWrap smooth);

	public:
		EnergyFunction*	pEnergyFunc;
	};
}

