# FightToTheEnd

使用UniRx、Photon、XLua搭建的第一人称射击游戏，既可以与机械人对战，亦可以联网与玩家对战，演示地址：[WebGL](https://qq306041575.github.io/FightToTheEnd)  

## 规则
W、A、S、D移动，1、2、3换武器，ESC弹出菜单，空格跳跃，左键射击，右键补子弹。  
生命值默认100，击中头部伤害是100，身体10，四肢1。被击中后默认有0.2秒冷却，会使玩家无法移动，而机器人则无法射击。  
游戏中获得武器后会马上装备，但在射击或补子弹时获得会被取消。默认装备手枪，当获得冲锋枪或散弹枪后，可按数字2更换第二件武器。  

## 联网
由于登录功能还没有实现，所以进入游戏后会随机分配一个玩家名字。  
联网对战要求至少两个玩家才能开始，可以开启两个不同的浏览器进行体验。  
因为使用了Photon的免费版，所以某些时段网络延迟会很高，可能需要多次尝试才会连接成功。如果想要流畅的游戏体验，可以在自己的服务器上搭建Photon的服务端，再修改游戏里PhotonServerSettings的配置，就能改善网络。  

## 截图
![screenshot](/Screenshots/editor.jpg)  
![screenshot](/Screenshots/browser.jpg)  
