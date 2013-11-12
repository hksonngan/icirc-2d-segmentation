#pragma once

#include "mrf.h"

namespace ManagedMRF {

	public ref class GraphCutWrap
	{	
	public:
		GraphCutWrap(void) { }
		GraphCutWrap(int XNum, int YNum, double DataEnergy[], double SmoothnessLabel[], 
			double SmoothnessHorizontal[], double SmoothnessVertical[], bool IsSwap);
		// Deallocate the native object on a destructor
	    ~GraphCutWrap() { delete pGC; }
	protected:
		// Deallocate the native object on the finalizer just in case no destructor is called
		!GraphCutWrap() { delete pGC; }

	public:
		void Initialize() { pGC->initialize(); }
		void ClearAnswer() { pGC->clearAnswer(); }
		double GetTotalEnergy() { return pGC->totalEnergy(); }
		void OptimizeOneIteration();
		int GetLabel(int CurrentPixelIndex) { return pGC->getLabel(CurrentPixelIndex); }

	private:
		MRF* pGC;
	};

}


