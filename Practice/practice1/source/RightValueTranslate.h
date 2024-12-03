#pragma once
/*
右值引用在传递时，如果使用了模板，在忘记了std::move时,不会报编译问题。
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
		registerCallback(func);		// 这一行不会报错。但我们应该使用std::move.
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
		// 本意为了在test1函数栈退了之后，再来一个栈写入点数据。但是因为函数栈占用的地址，和lambda捕获的地址所在较远，所以同样也无法复现问题。
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
	* 在当前项目中，因为项目比较简单，无法复现出问题，
	* 在复杂项目中，值捕获的temp2的值，有可能在真正执行callback时，temp2的值不对了。
	* 需要记得使用std::move
	*/
};