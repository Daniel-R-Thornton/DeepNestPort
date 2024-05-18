#include <iostream>
#include <boost/polygon/polygon.hpp>
  
extern  "C" {  	
#ifdef __GNUC__
#define EXPORTED_DEF __attribute__((visibility("default")))
#else
#define EXPORTED_DEF __declspec(dllexport)  
#endif


	EXPORTED_DEF void  setData(int cntA, double* pntsA, int holesCnt, int* holesSizes, double* holesPoints, int cntB, double* pntsB);
	EXPORTED_DEF void  getSizes1(int* sizes);
	EXPORTED_DEF void  getSizes2(int* sizes1, int* sizes2);
	EXPORTED_DEF void  getResults(double* data, double* holesData);
	EXPORTED_DEF void  calculateNFP();
}
