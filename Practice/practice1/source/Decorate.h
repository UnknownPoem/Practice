#pragma once
//װ����ģʽ

#include "head.h"

using namespace std;

class Phone
{
public:
	Phone()
	{}
	virtual ~Phone()
	{}
	virtual void showDecorate()
	{}
};
class iPhone : public Phone  //�����ֻ���
{
private:
	string name;
public:
	iPhone(string _name)
		:name(_name)
	{}
	~iPhone()
	{}
	void showDecorate()
	{
		cout << name << "��װ��" << endl;
	}
};
class NokiaPhone : public  Phone
{
private:
	string name;
public:
	NokiaPhone(string _name)
		:name(_name)
	{}
	~NokiaPhone()
	{}
	void  showDecorate()
	{
		cout << name << "��װ��" << endl;
	}
};

class DecoratorPhone :public Phone
{
private:
	Phone* m_phone; //Ҫװ�ε��ֻ�
public:
	DecoratorPhone(Phone* phone)
		:m_phone(phone)
	{}
	virtual void showDecorate()
	{
		m_phone->showDecorate();
	}
};

class DecoratePhoneA : public DecoratorPhone //�����װ��A
{
public:
	DecoratePhoneA(Phone* ph)
		:DecoratorPhone(ph)
	{}
	void showDecorate()
	{
		DecoratorPhone::showDecorate();
		AddDecorate();
	}
private:
	void AddDecorate()
	{
		cout << "���ӹҼ�" << endl;
	}
};
class DecoratePhoneB : public DecoratorPhone
{
public:
	DecoratePhoneB(Phone* ph)
		:DecoratorPhone(ph)
	{}
	void showDecorate()
	{
		DecoratorPhone::showDecorate();
		AddDecorate();
	}
private:
	void  AddDecorate()
	{
		cout << "��Ļ��Ĥ" << endl;
	}
};

int test_Decorate()  //װ����ģʽ
{
	Phone* ph = new NokiaPhone("16300");
	Phone* dpa = new DecoratePhoneA(ph);//���ӹҼ�
	Phone* dpb = new DecoratePhoneB(dpa);//������Ĥ
	dpb->showDecorate();

	delete ph;
	delete dpa;
	delete dpb;
	system("pause");
	return 0;
}