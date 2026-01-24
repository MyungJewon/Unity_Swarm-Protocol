# Project: Legion Core 
### Mobile Massive Unit Battle Simulator (Tech Demo)
> **Extreme Mobile Optimization Showcase**: Rendering 10,000+ dynamic units at 60FPS on Mobile Devices.

![Badge](https://img.shields.io/badge/Unity-6000.3.0f1-black?logo=unity)
![Badge](https://img.shields.io/badge/Platform-Android-green)
![Badge](https://img.shields.io/badge/Tech-DOTS%20%26%20GPU%20Instancing-blue)

---

## Project Overview (개요)
**Legion Core**는 모바일 디바이스의 하드웨어 한계를 시험하고 극복하기 위해 개발된 **대규모 실시간 전투 시뮬레이션 프로젝트**입니다.
일반적인 객체 지향(OOP) 방식으로는 모바일에서 수백 마리의 유닛만 등장해도 프레임 드랍이 발생합니다. 본 프로젝트는 **데이터 지향 기술 스택(DOTS)**과 **GPU Instancing**, 그리고 **Custom Spatial Partitioning**을 도입하여, **10,000개 이상의 유닛이 개별 AI로 동작하는 상황에서도 안정적인 60FPS를 유지**하는 것을 목표로 했습니다.

### Key Objectives
* **Zero Allocation:** 런타임 중 GC(Garbage Collection) 스파이크 0% 달성.
* **Maximize Throughput:** Unity Job System과 Burst Compiler를 활용한 멀티스레딩 연산.
* **GPU Bound Rendering:** CPU 병목을 제거하기 위한 `DrawMeshInstancedIndirect` 및 Compute Shader 활용.

---

## Technical Deep Dive (핵심 기술 명세)

### 1. Rendering Optimization: GPU Instancing & VAT
기존 `SkinnedMeshRenderer`의 CPU 스키닝 비용을 제거하기 위해 **Vertex Animation Texture (VAT)** 기법을 적용했습니다.
* **Pre-baking:** 애니메이션의 정점(Vertex) 위치 데이터를 텍스처로 미리 베이킹.
* **Shader:** 텍스처에서 정점 위치를 읽어와 Vertex Shader 단계에서 애니메이션 처리.
* **Indirect Drawing:** `Graphics.DrawMeshInstancedIndirect` API를 사용하여 **단 1회의 Draw Call**로 1만 개의 유닛을 렌더링.

### 2. Physics & Logic: Custom Spatial Hashing
Unity 내장 Physics(PhysX)는 수만 개의 충돌체 연산에 적합하지 않습니다. 이를 대체하기 위해 가벼운 **자체 공간 분할 알고리즘**을 구현했습니다.
* **Spatial Partitioning:** 맵을 그리드 셀로 나누고, 유닛의 좌표를 해싱(Hashing)하여 인접한 유닛만 탐색.
* **R&D Background:** 과거 **SLAM 및 LiDAR 포인트 클라우드 처리 경험**을 응용하여, 대량의 좌표 데이터를 효율적으로 쿼리하는 구조 설계.

### 3. Multi-threading: Job System & Burst
메인 스레드의 부하를 줄이기 위해 모든 유닛의 로직(이동, 회전, 타겟 탐색)을 병렬 처리했습니다.
* **IJobEntity:** 유닛의 데이터를 구조체(Struct) 기반의 `NativeArray`로 관리하며 메모리 레이아웃 최적화.
* **Burst Compiler:** C# 코드를 고도로 최적화된 네이티브 코드로 컴파일하여 연산 속도 10배 이상 향상.


<!--
---
## Performance Metrics (성능 지표)
*Test Device: Galaxy S21 / iPhone 13*

| Metric | Traditional OOP | **Legion Core (DOTS)** | Improvement |
| :--- | :---: | :---: | :---: |
| **Active Units** | 500 | **10,000+** | **20x** 🚀 |
| **FPS** | 25 (Unstable) | **60 (Fixed)** | **Stable** |
| **Draw Calls** | ~800 | **~15** | **98% Reduced** |
| **Batches** | ~1,000 | **~20** | **Optimization** |

> *스크린샷: 좌측 상단 프로파일러 수치 확인*
> ![Performance Screenshot](Place_Your_Screenshot_Here.png)
---
-->


## Architecture (아키텍처)
이 프로젝트는 철저한 **데이터 지향(Data-Oriented)** 설계 원칙을 따릅니다.

```mermaid
graph TD
    A[Game Manager] -->|Schedule| B(Job System)
    B -->|Parallel Execute| C{Logic Jobs}
    C -->|Update Transform| D[Move Job]
    C -->|Query Grid| E[Collision Job]
    C -->|Update State| F[AI Job]
    
    G[Render Manager] -->|Read NativeArrays| H(Compute Buffer)
    H -->|Dispatch| I[Indirect Draw Args]
    I -->|Render| J[GPU Instancing Shader]
