#pragma once

#include "head.h"

class LambdaRefTest {
public:
	class testTool1 {
	public:
		int n = 1;
		testTool1() {
			cout << "testTool1 construct" << n << endl;
		}
		testTool1& operator=(const testTool1& object) {
			cout << "testTool1 operator=" << n << endl;
		}
	};
	class testTool2 {
	public:
		int n = 1;
		testTool2() {
			cout << "testTool2 construct" << n << endl;
		}
		testTool2& operator=(const testTool2& object) {
			cout << "testTool2 operator=" << n << endl;
		}
	};


	void testLambdaRefTest() {
		testTool1 a;
		auto lambda1 = [=]() {
			testTool2 b;
			//++a.n;
			auto lambda2 = [=]() {
				//++b.n;
				cout << a.n << "\t" << b.n << endl;
				};

			lambda2();
			};

		lambda1();
		cout << a.n << endl;
	}
};
