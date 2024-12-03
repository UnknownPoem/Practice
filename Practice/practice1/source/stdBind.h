#pragma once
/*
��ֵ�����ڴ���ʱ�����ʹ����ģ�壬��������std::moveʱ,���ᱨ�������⡣
*/
#include "head.h"

class StdBind {
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
	private:
		TLambda mLambda;
	};

	template<typename TLambda>
	class TTaskFunction1 : public TaskFunction
	{
	public:
		TTaskFunction1(TLambda&& lambda)
			: mLambda(lambda)
		{}

		template <typename... Args>
		void Execute(Args&... rest)
		{
			mLambda(rest...);
		}
	private:
		TLambda mLambda;
	};

public:
	std::vector<std::shared_ptr<TaskFunction>> callbackFunctions;

	template<typename TLambda>
	void registerCallback(TLambda&& func) {
		callbackFunctions.emplace_back(std::make_shared<TTaskFunction<TLambda>>(std::forward<TLambda>(func)));
	}

	void executeAll() {
		for (auto func : callbackFunctions) {
			func->Execute();
		}
	}

	void test() {
		registerCallback([]() { cout << "djfk"; });
	}
};