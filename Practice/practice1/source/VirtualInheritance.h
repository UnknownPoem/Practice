// ��̳�
// https://blog.csdn.net/Jdxxwu/article/details/143181746
// ���������̳�ԭ����Ҫ��Ҫ���ڴ棬����������ƫ����

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