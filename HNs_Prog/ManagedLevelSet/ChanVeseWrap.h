// ManagedLevelSet.h

#pragma once

#include "VpChanVeseACM.h"

namespace ManagedLevelSet {

	public ref class ChanVeseWrap
	{
	public:
		ChanVeseWrap(void) { }
		ChanVeseWrap(int XNumPara, int YNumPara, short SrcDensityVolume[]);
		// Deallocate the native object on a destructor
	    ~ChanVeseWrap() { pACM->Release(); delete pACM; }
	protected:
		// Deallocate the native object on the finalizer just in case no destructor is called
		!ChanVeseWrap() { pACM->Release(); delete pACM; }
	public:
		void SetParameters(float Mu, float Lamda, float DeltaT, int hx, int hy, int IterNum)
			{ pACM->InitParam(Mu, Lamda, DeltaT, hx, hy, 1, IterNum, CVpChanVeseACM<short>::L2); }
		void SetInitialObject(BYTE SrcMaskVolume[]);
		bool Run(BYTE DesMaskVolume[]);
		void GetOnlyObject(BYTE DesMaskVolume[]);

	private:
		CVpChanVeseACM<short>* pACM;
		int XNum, YNum;
	};
}
