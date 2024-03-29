# Shader Variants几个注意事项

以下内容基于Unity2019.4.10讨论。



## 收集(Collection)

现在而言，变体收集的目的通常有这些：1、确保不会出现变体丢失的情况；2、辅助预热。

利用到的机制：Unity打包时，会根据依赖关系进行打包。

现在Unity提供了一个.shadervariants类型的文件（ ShaderVariantCollection ，以下称其为SVC），我们可以通过创建SVC文件，并将其和shader打在同一个包内，来收集shader变体。



### 官方收集变体方式

Unity官方给出的收集方式是，在Project Setting->Graphics中，用save to asset..和clear两个按扭去生成SVC，具体来说：先clear，然后尽可能地play遍历到游戏内的各个场景的各种情况，在此过程中遇到的shader variants会被记录下来，使用save to asset..即可保存成一个svc。

这一方式的问题在于

1. 它的收集有时是不正确的（会多一些并没有用到的变体）
1. 这种方法我们的控制力度不足，而且通过遍历的方式容易出遗漏。

于是，需要写一个工具去做这个事情。



收集
        需要手动添加材质路径，工具将收集路径下所有材质引用到的shader。
        需要手动添加动态关键字，工具将会对这些关键字做排列组合。
        需要手动添加场景，工具将会依照场景与shader的引用关系，解析并配置内置关键字。
        一键收集变体只会把收集到的添加进SVC，并不会在此之前清空，如果希望覆盖，手动在收集前清空即可。



收集时，对关键字的解析采用逐pass的粒度

考虑到两方面：

1、2021版本提供了按pass获取keyword接口方法

2、逐shader的粒度可能确实会多收集一些没用到的变体。有些关键字并不是在所有pass都声明使用的。



## 剔除（Strip）

变体剔除的目的通常是为了：1、减少包体大小；2、缩短打包时间；3、减少运行时内存开销。

我们剔除通常要利用到IPreprocessShaders.OnProcessShader接口

```
public void OnProcessShader(Shader shader, Rendering.ShaderSnippetData snippet, IList<ShaderCompilerData> data); 
```

然后通过对data的操作，进行剔除。



我们可以用哪些条件做剔除呢？先思考下面的问题。

### 问题：假设我们继承了此接口，并遍历data加打印，对于一个shader，会打印到多少遍？

打印到的遍数 = 

shader_feature变体数量 * multi_compile变体数量 * shaderType数 * shaderCompilerPlatform数 * Pass数量总加和 * graphicsTier数



**shader_feature变体数量** = 每个shader_feature声明会打包的变体数量的累乘积

每个shader_feature声明会打包的变体数量 =  默认关键字+被引用到的关键字（去掉重复）

假如我们声明了

```
#pragma shader_feature _ A1 A2 A3
#pragma shader_feature _ B1 B2 B3
```

并在本次打包时，使用了一个引用了以下变体组合SVC

```
A2B2、B1
```

对于A，有两个关键字会被打包：空、A2

对于B，有三个关键字会被打包：空、B1、B2

其中，空为默认关键字。



**multi_compile变体数量** = 每个multi_compile声明会打包的变体数量的累乘积

每个multi_compile声明会打包的变体数量 = 声明的所有关键字

**shaderType**：看具体情况，可能有
![shaderType](res\shaderType.jpg)

**shaderCompilerPlatform**：看具体情况，可能有
![shaderType](res\shaderCompilerPlatform.jpg)

**pass**：看具体情况

**graphicsTier**：高中低三档



因此，剔除时可以考虑以上条件，进行我们的剔除工作。

这里打算采取的策略是：compilePlatform依照项目配置，不做额外的剔除；PassType、Keywords依照SVC进行剔除，不在SVC内的均剔除掉。



有一个说法：关于内置管线的tiers。如果把它们都设置成一样的，可能只打一份，能减少一些包体大小。

built-in shader这方面，可以依据项目的实际情况进行缩减。



## Unity给了几个剔除选项（light、fog、instance）

经过测试，Unity的设置选项会先于我们调用的**OnProcessShader**。

我们打算采取的策略是：先收集最全的，然后再项目组剔除选项以供剔除。

所以这里的设置需要是不做任何剔除。



考虑到项目人员在打包时不一定会想到调整这些设置，应该使用反射在打包前把GraphicSetting内的这些选项设置正确，打包后再还原。





## 预热（WarmUp）

变体预热做的事情是：提前解析shader，把可能用到的变体送入GPU，以减少运行时卡顿。



如果不预热，那么开销大多来源于两件事情：

1. Shader.Parse
2. Shader.CreateGPUProgram



尝试用以下几种方式进行预热

1. 使用ShaderVariantCollection.WarmUp()
   1. 若multi_compile收集不全，则会出现没预热到的问题；
   2. 最推荐
2. 使用AssetBundle.LoadAllAssets<Shader>()
   1. Shader.Parse的开销没了
   2. Shader.CreateGPUProgram的开销仍在。
3. 使用Shader.WarmAllShader()
   1. 从真实测试中看，发现它可能预热的不全。
   2. 从表现上看，它的机制是先清除已创建的GPUProgram，再重新创建。又因为它预热不全的缘故，可能会产生已加载过的变体会重复产生开销的情况。
   3. 不推荐。







## 附

### 反射调用

先定位想调用的方法（用反编译dll或者直接看Unity给出的UnityCSReference）

反编译工具：.NET Reflector



然后，比如说，如果我们想看SVC相关的东西，找到ShaderVariantCollectiionInspector.cs。然后看代码。



考虑反射调用shader.FindPassTagValue 来获取每个pass所设置的LightMode名称



## 可能的坑

我们的项目组采用 bundle.loadAllAssets <Shader>，同时拿一个字典持有所有shader，莫名地比svc.WarmUp多很多。猜测是有些shader采样过多，有opengles报错，导致的。