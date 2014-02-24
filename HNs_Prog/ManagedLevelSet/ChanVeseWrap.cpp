// This is the main DLL file.

#include "Stdafx.h"
#include "ChanVeseWrap.h"

namespace ManagedLevelSet {

	ChanVeseWrap::ChanVeseWrap(int XNumPara, int YNumPara, short SrcDensityVolume[])
	{
		XNum = XNumPara;
		YNum = YNumPara;
		short** Densitybuffer = new short*[1];
		Densitybuffer[0] = new short[XNum * YNum];
		memcpy(Densitybuffer[0], SrcDensityVolume, sizeof(short) * XNum * YNum);
		pACM = new CVpChanVeseACM<short>();
		pACM->Load(XNum, YNum, 1, Densitybuffer);
	}

	void ChanVeseWrap::SetInitialObject(BYTE SrcMaskVolume[])
	{
		BYTE** SrcMaskbuffer = new BYTE*[1];
		SrcMaskbuffer[0] = new BYTE[XNum * YNum];
		memcpy(SrcMaskbuffer[0], SrcMaskVolume, sizeof(BYTE) * XNum * YNum);
		pACM->SetMask(SrcMaskbuffer); 
		pACM->InitPhi(CVpChanVeseACM<short>::SMALLMANY);
	}

	bool ChanVeseWrap::Run(BYTE DesMaskVolume[])
	{ 
		BYTE** DesMaskbuffer = new BYTE*[1];
		DesMaskbuffer[0] = new BYTE[XNum * YNum];
		memcpy(DesMaskbuffer[0], DesMaskVolume, sizeof(BYTE) * XNum * YNum);
		bool bResult = pACM->Do(DesMaskbuffer);
		if (bResult)
			memcpy(DesMaskVolume, DesMaskbuffer[0], sizeof(BYTE) * XNum * YNum);
		return bResult;
	}

	void ChanVeseWrap::GetOnlyObject(BYTE DesMaskVolume[])
	{
		BYTE** DesMaskbuffer = new BYTE*[1];
		DesMaskbuffer[0] = new BYTE[XNum * YNum];
		memcpy(DesMaskbuffer[0], DesMaskVolume, sizeof(BYTE) * XNum * YNum);
		pACM->SetOnlyObject(DesMaskbuffer);
		memcpy(DesMaskVolume, DesMaskbuffer[0], sizeof(BYTE) * XNum * YNum);
	}
}
