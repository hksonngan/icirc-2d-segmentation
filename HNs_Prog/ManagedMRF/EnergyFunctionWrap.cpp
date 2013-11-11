#include "StdAfx.h"
#include "EnergyFunctionWrap.h"

namespace ManagedMRF {

	/*
	MRF::CostVal CostFunction(int pix1, int pix2, int i, int j)
	{
		if (pix2 < pix1) { // ensure that fnCost(pix1, pix2, i, j) == fnCost(pix2, pix1, j, i)
		int tmp;
		tmp = pix1; pix1 = pix2; pix2 = tmp; 
		tmp = i; i = j; j = tmp;
		}
		MRF::CostVal answer = (pix1*(i+1)*(j+2) + pix2*i*j*pix1 - 2*i*j*pix1) % 100;
		return answer / 10;
	}*/

	EnergyFunctionWrap::EnergyFunctionWrap(DataCostWrap data, SmoothnessCostWrap smooth)
	{
		pEnergyFunc = new EnergyFunction(data.pEnergyFunc, smooth.pEnergyFunc);
	}

	DataCostWrap::DataCostWrap(double DataEneryArray[])
	{
		pEnergyFunc = new DataCost(DataEneryArray);
	}

	SmoothnessCostWrap::SmoothnessCostWrap()
	{
		//pEnergyFunc = new SmoothnessCost((MRF::SmoothCostGeneralFn)CostFunction);
	}


}

