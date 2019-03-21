# FulltextRetrievalSystem-mulver
毕业设计-全文检索系统-分布式版本

在单机版的基础上，使用Orleans 1.5.3框架和Redis 3.2.100 Windows ver实现分布式检索。  
**由于安全提示，packages.config的 <package id="Microsoft.Data.OData" version="5.8.4" targetFramework="net461" /> 原版本为5.6.4，如有问题可以改回原版本**  

**需要注意：VS调试要开启IIS64位版本！**  

LocalDB存放着本地检索数据库文件夹，有部分爬虫爬取的测试数据在里面。  
FulltextRetrievalSystem/packages/Xapian.1.2.23/的_XapianSharp.dll和zlib1.dll需手动复制到执行文件夹下，包括Orleans的服务器文件夹下，修改的xapian代码见本人的XapianModified项目；除此之外，jieba.net的resource文件夹也要复制到同一文件夹下，否则中文解析会出错。  
CrawlPart是爬虫操作相关文件夹，PushFunction是邮件推送文件夹，ScheduledTask是任务爬虫和任务推送文件夹，XapianPart是数据库文件夹，WebView是网站，WebCommon是数据交互层，GrainInterfaces和Grain是Orleans相关内容，Cache是redis和Orleans交互模块，OrleansHost是Orleans服务器。    
如果要配置路径之类的东西，请注意修改web.config或者app.config的对应内容！  
登录管理员账号admin@123.com，密码是Pa$$w0rd，随便玩，反正没啥权限……  
代码天长日久没动过了，今天把它们拿出来通通风，如果有bug，这是很可能的，请见谅！  
