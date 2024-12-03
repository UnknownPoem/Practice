// 虚继承
// https://blog.csdn.net/Jdxxwu/article/details/143181746
// 深入理解虚继承原理，主要是要看内存，理解虚基表与偏移量

#include"head.h"

namespace VirtualInheritance {
	class A
	{
	public:
		int _a;
	};

	// class B : public A
	class B : virtual public A
	{
	public:
		int _b;
	};

	// class C : public A
	class C : virtual public A
	{
	public:
		int _c;
	};

	class D : public B, public C
	{
	public:
		int _d;
	};


};