#pragma once
/*
��ֵ�����ڴ���ʱ�����ʹ����ģ�壬��������std::moveʱ,���ᱨ�������⡣
*/
#include "head.h"

class RightValueTranslate {
	class TaskFunction {
	public:
		TaskFunction() = default;
		virtual ~TaskFunction() = default;
		virtual void Execute() = 0;
	};

	template<typename TLambda>
	class TTaskFunction : public TaskFunction
	{
	public:
		TTaskFunction(TLambda&& lambda)
			: mLambda(lambda)
		{}
		void Execute() override
		{
			mLambda();
		}

		//private:
		TLambda mLambda;
	};
public:
	class RightValueTranslate2 {
	public:
		RightValueTranslate2() {
			memset(&str, 'y', 65536);
		}
		void printAddress() {
			cout << "this RightValueTranslate2 class's address range is " << (void*)(this) << " to " << (void*)(&str[65535]) << endl;
		}
		char str[65536];
	};


public:
	// data field
	std::vector<std::shared_ptr<TaskFunction>> functions;

	// function field
	template<typename TLambda>
	void registerCallback(TLambda&& func) {
		//func();
		functions.emplace_back(std::make_shared<TTaskFunction<TLambda>>(std::forward<TLambda>(func)));
		char* temp = (char*)(functions[0].get());
		temp = temp + 8;
		std::cout << "pause point: mLambda.str = " << temp << "\taddress = " << (void*)temp << endl;
	}

	template<typename TLambda>
	void process(TLambda&& func) {
		registerCallback(func);		// ��һ�в��ᱨ��������Ӧ��ʹ��std::move.
	}

	void executeAll() {
		for (auto func : functions) {
			func->Execute();
		}
	}

	void test1() {
		struct Temp {
			char str[16] = { "abc" };
		};
		Temp temp;
		cout << "test1 temp's address is " << (void*)(temp.str) << endl;
		registerCallback([temp2 = temp]()->void {
			std::cout << temp2.str << std::endl;
			});
	}

	void test2() {
		// ����Ϊ����test1����ջ����֮������һ��ջд������ݡ�������Ϊ����ջռ�õĵ�ַ����lambda����ĵ�ַ���ڽ�Զ������ͬ��Ҳ�޷��������⡣
		struct Temp {
			char str[65535];
		};
		Temp temp;
		memset(&temp.str, 'z', 65535);
		cout << "test2 temp's address is " << (void*)(temp.str) << endl;
	}

	void test() {
		cout << "this point address = " << (void*)(this) << endl;
		test1();
		test2();
		executeAll();
	}
	/*
	* �ڵ�ǰ��Ŀ�У���Ϊ��Ŀ�Ƚϼ򵥣��޷����ֳ����⣬
	* �ڸ�����Ŀ�У�ֵ�����temp2��ֵ���п���������ִ��callbackʱ��temp2��ֵ�����ˡ�
	* ��Ҫ�ǵ�ʹ��std::move
	*/
};